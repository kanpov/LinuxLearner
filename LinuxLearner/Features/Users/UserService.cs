using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Groups;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
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
    private const int MaxPageSize = 10;
    
    public async Task<UserDto> GetAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return MapToUserDto(user);
    }
    
    public async Task<UserDto> PatchAuthorizedUserAsync(HttpContext httpContext, UserPatchDto userPatchDto)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        
        ProjectUserPatchDto(user, userPatchDto);
        await userRepository.UpdateUserAsync(user);

        return MapToUserDto(user);
    }

    public async Task DeleteAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        await userRepository.DeleteUserAsync(user.Name);
    }

    public async Task<UserDto?> GetUserAsync(string username)
    {
        var user = await userRepository.GetUserAsync(username);
        return user is null ? null : MapToUserDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(int page, int pageSize)
    {
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var users = await userRepository.GetUsersAsync(page, pageSize);
        return users.Select(MapToUserDto);
    }

    public async Task<bool> ElevateUserAsync(HttpContext httpContext, string username)
    {
        var senderUser = await GetAuthorizedUserEntityAsync(httpContext);
        var receiverUser = await userRepository.GetUserAsync(username);

        if (receiverUser is null
            || senderUser.UserType <= receiverUser.UserType
            || receiverUser.UserType == UserType.Admin) return false;

        receiverUser.UserType = receiverUser.UserType == UserType.Student ? UserType.Teacher : UserType.Admin;
        await userRepository.UpdateUserAsync(receiverUser);

        var metadataOptions = configuration.GetRequiredSection("KeycloakMetadata");
        var realmId = metadataOptions["Realm"]!;

        var userId = await fusionCache.GetOrSetAsync<string>(
            $"/keycloak-user-id/{receiverUser.Name}",
            async token =>
            {
                var queriedUsers = await keycloakUserClient.GetUsersAsync(realmId,
                    new GetUsersRequestParameters
                    {
                        Max = 1,
                        Username = receiverUser.Name,
                        Exact = true,
                        BriefRepresentation = true
                    }, token);
                return queriedUsers.First().Id!;
            });
        
        var groupName = receiverUser.UserType switch
        {
            UserType.Teacher => metadataOptions["TeacherGroup"]!,
            _ => metadataOptions["AdminGroup"]!
        };
        var groupId = await fusionCache.GetOrSetAsync<string>(
            $"/keycloak-group-id/{groupName}",
            async token =>
            {
                var queriedGroups = await keycloakGroupClient.GetGroupsAsync(
                    realmId, new GetGroupsRequestParameters
                    {
                        Search = groupName,
                        Exact = false,
                        Max = 1,
                        BriefRepresentation = true
                    }, token);
                return queriedGroups.First().Id!;
            });
        
        await keycloakUserClient.JoinGroupAsync(realmId, userId, groupId);

        return true;
    }

    private async Task<User> GetAuthorizedUserEntityAsync(HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;
        
        var username = claimsPrincipal.Identity!.Name!;
        var user = await userRepository.GetUserAsync(username);
        var claimValue = claimsPrincipal.Claims.First(c => c.Type == "resource_access").Value;
        var userType = UserType.Admin;
        
        if (claimValue.Contains("student")) userType = UserType.Student;
        else if (claimValue.Contains("teacher")) userType = UserType.Teacher;

        if (user is not null) return user;

        var newUser = new User
        {
            Name = username,
            UserType = userType,
            Description = null,
            RegistrationTime = DateTimeOffset.UtcNow
        };
        await userRepository.AddUserAsync(newUser);

        return newUser;
    }

    public static UserDto MapToUserDto(User user) =>
        new(user.Name, user.UserType, user.Description, user.RegistrationTime);

    private static void ProjectUserPatchDto(User user, UserPatchDto userPatchDto)
    {
        user.Description = userPatchDto.Description;
    }
}