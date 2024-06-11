using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Requests.Groups;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
    public static bool KeycloakAvailable { private get; set; } = true;
    
    public async Task<UserDto> GetAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return MapToUserDto(user);
    }
    
    public async Task<UserDto> PatchAuthorizedUserAsync(HttpContext httpContext, UserPatchDto userPatchDto)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return (await PatchUserAsync(user.Name, userPatchDto))!;
    }

    public async Task<UserDto?> PatchUserAsync(string username, UserPatchDto userPatchDto)
    {
        var user = await userRepository.GetUserAsync(username);
        if (user is null) return null;
        
        ProjectUserPatchDto(user, userPatchDto);
        await userRepository.UpdateUserAsync(user);

        return MapToUserDto(user);
    }

    public async Task DeleteAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        await DeleteUserAsync(user.Name);
    }
    
    public async Task<bool> DeleteUserAsync(string username)
    {
        var user = await userRepository.GetUserAsync(username);
        if (user is null) return false;

        await userRepository.DeleteUserAsync(username);

        if (KeycloakAvailable)
        {
            var userId = await GetKeycloakUserId(username);
            await keycloakUserClient.DeleteUserAsync("master", userId);
        }

        return true;
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

    public async Task<bool> ChangeUserRoleAsync(HttpContext httpContext, string username, bool demote)
    {
        var senderUser = await GetAuthorizedUserEntityAsync(httpContext);
        var receiverUser = await userRepository.GetUserAsync(username);
        
        var metadataOptions = configuration.GetRequiredSection("KeycloakMetadata");
        var realmId = metadataOptions["Realm"]!;
        
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

        if (KeycloakAvailable)
        {
            var newGroupId = await GetKeycloakGroupId(receiverUser.UserType, metadataOptions);
            var userId = await GetKeycloakUserId(receiverUser.Name);

            await keycloakUserClient.JoinGroupAsync(realmId, userId, newGroupId);

            if (demote)
            {
                var oldGroupId = await GetKeycloakGroupId(oldUserType, metadataOptions);
                await keycloakUserClient.LeaveGroupAsync(realmId, userId, oldGroupId);
            }
        }

        return true;
    }

    private async Task<string> GetKeycloakGroupId(UserType userType, IConfigurationSection metadataOptions)
    {
        var groupName = userType switch
        {
            UserType.Student => metadataOptions["StudentGroup"]!,
            UserType.Teacher => metadataOptions["TeacherGroup"]!,
            _ => metadataOptions["AdminGroup"]!
        };
        return await fusionCache.GetOrSetAsync<string>(
            $"/keycloak-group-id/{groupName}",
            async token =>
            {
                var queriedGroups = await keycloakGroupClient.GetGroupsAsync(
                    "master", new GetGroupsRequestParameters
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

    private async Task<string> GetKeycloakUserId(string username)
    {
        return await fusionCache.GetOrSetAsync<string>(
            $"/keycloak-user-id/{username}",
            async token =>
            {
                var queriedUsers = await keycloakUserClient.GetUsersAsync("master",
                    new GetUsersRequestParameters
                    {
                        Max = 1,
                        Username = username,
                        Exact = true,
                        BriefRepresentation = true
                    }, token);
                return queriedUsers.First().Id!;
            },
            new FusionCacheEntryOptions(TimeSpan.FromDays(7)));
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