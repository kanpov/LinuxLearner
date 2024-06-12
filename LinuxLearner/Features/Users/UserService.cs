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
    
    public async Task<UserDto> GetAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return MapToUserDto(user);
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
        return user is null ? null : MapToUserDto(user);
    }

    public async Task<bool> ChangeUserRoleAsync(HttpContext httpContext, Guid userId, bool demote)
    {
        var senderUser = await GetAuthorizedUserEntityAsync(httpContext);
        var receiverUser = await userRepository.GetUserAsync(userId);
        
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

        var newGroupId = await GetKeycloakGroupId(receiverUser.UserType);

        await keycloakUserClient.JoinGroupAsync(realmId, userId.ToString(), newGroupId);

        if (demote)
        {
            var oldGroupId = await GetKeycloakGroupId(oldUserType);
            await keycloakUserClient.LeaveGroupAsync(realmId, userId.ToString(), oldGroupId);
        }

        return true;
    }

    public async Task<string> GetKeycloakGroupId(UserType userType)
    {
        var metadataOptions = configuration.GetRequiredSection("KeycloakMetadata");
        
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

    public async Task<string> GetKeycloakUserId(string username)
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

        var userId = Guid.Parse(claimsPrincipal.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value);
        var user = await userRepository.GetUserAsync(userId);
        var resourceAccess = claimsPrincipal.Claims.First(c => c.Type == "resource_access").Value;
        var userType = UserType.Admin;
        
        if (resourceAccess.Contains("student")) userType = UserType.Student;
        else if (resourceAccess.Contains("teacher")) userType = UserType.Teacher;

        if (user is not null) return user;

        var newUser = new User
        {
            Id = userId,
            UserType = userType
        };
        await userRepository.AddUserAsync(newUser);

        return newUser;
    }

    public static UserDto MapToUserDto(User user) => new(user.Id, user.UserType);
}