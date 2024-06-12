using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Groups;
using LinuxLearner.Domain;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.Features.Users;

public class UserService(
    UserRepository userRepository,
    IKeycloakUserClient keycloakUserClient,
    IKeycloakGroupClient keycloakGroupClient,
    IConfiguration configuration,
    IFusionCache fusionCache)
{
    private readonly IConfigurationSection _metadataOptions = configuration.GetRequiredSection("KeycloakMetadata");
    private readonly string _realm = configuration.GetRequiredSection("KeycloakMetadata")["Realm"]!;
    
    public async Task<UserDto> GetAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return await FetchUserDtoAsync(user);
    }
    
    public async Task DeleteAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        await DeleteUserAsync(user.Id);
    }
    
    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await userRepository.GetUserAsync(userId);
        if (user is null) return false;

        await userRepository.DeleteUserAsync(userId);
        await keycloakUserClient.DeleteUserAsync("master", userId.ToString());

        return true;
    }

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await userRepository.GetUserAsync(userId);
        return user is null ? null : await FetchUserDtoAsync(user);
    }

    public async Task<bool> ChangeUserRoleAsync(HttpContext httpContext, Guid userId, bool demote)
    {
        var senderUser = await GetAuthorizedUserEntityAsync(httpContext);
        var receiverUser = await userRepository.GetUserAsync(userId);
        
        if (receiverUser is null || senderUser.UserType <= receiverUser.UserType) return false;
        if (demote && receiverUser.UserType == UserType.Student) return false;
        if (!demote && receiverUser.UserType == UserType.Admin) return false;

        var oldUserType = receiverUser.UserType;

        if (demote)
        {
            receiverUser.UserType = receiverUser.UserType switch
            {
                UserType.Admin => UserType.Teacher,
                _ => UserType.Student
            };
        }
        else
        {
            receiverUser.UserType = receiverUser.UserType switch
            {
                UserType.Student => UserType.Teacher,
                _ => UserType.Admin
            };
        }

        await userRepository.UpdateUserAsync(receiverUser);

        var newGroupId = await GetKeycloakGroupId(receiverUser.UserType);

        await keycloakUserClient.JoinGroupAsync(_realm, userId.ToString(), newGroupId);

        if (demote)
        {
            var oldGroupId = await GetKeycloakGroupId(oldUserType);
            await keycloakUserClient.LeaveGroupAsync(_realm, userId.ToString(), oldGroupId);
        }

        return true;
    }

    public async Task<string> GetKeycloakGroupId(UserType userType)
    {
        var groupName = userType switch
        {
            UserType.Student => _metadataOptions["StudentGroup"]!,
            UserType.Teacher => _metadataOptions["TeacherGroup"]!,
            _ => _metadataOptions["AdminGroup"]!
        };
        return await fusionCache.GetOrSetAsync<string>(
            $"/keycloak-group-id/{groupName}",
            async token =>
            {
                var queriedGroups = await keycloakGroupClient.GetGroupsAsync(
                    _realm, new GetGroupsRequestParameters
                    {
                        Search = groupName,
                        Exact = false,
                        Max = 1,
                        BriefRepresentation = true
                    }, token);
                return queriedGroups.First().Id!;
            },
            new FusionCacheEntryOptions(TimeSpan.FromDays(7)));
    }

    public async Task<User> GetAuthorizedUserEntityAsync(HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;

        var userId = Guid.Parse(claimsPrincipal.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value);
        var user = await userRepository.GetUserAsync(userId);
        var resourceAccess = claimsPrincipal.Claims.First(c => c.Type == "resource_access").Value;
        var userType = UserType.Student;

        if (resourceAccess.Contains("teacher")) userType = UserType.Teacher;
        if (resourceAccess.Contains("admin")) userType = UserType.Admin;

        if (user is not null) return user;

        var newUser = new User
        {
            Id = userId,
            UserType = userType
        };
        await userRepository.AddUserAsync(newUser);

        return newUser;
    }

    internal async Task<UserDto> FetchUserDtoAsync(User user)
    {
        return await fusionCache.GetOrSetAsync<UserDto>($"/user-dto/{user.Id}",
            async token =>
            {
                var keycloakUser = await keycloakUserClient.GetUserAsync(_realm, user.Id.ToString(), false, token);
                var description = keycloakUser.Attributes?
                    .FirstOrDefault(a => a.Key == "description")
                    .Value
                    .FirstOrDefault();
                
                return new UserDto(
                    user.Id,
                    user.UserType,
                    keycloakUser.Username!,
                    keycloakUser.FirstName,
                    keycloakUser.LastName,
                    keycloakUser.Email,
                    description);
            });
    }
}