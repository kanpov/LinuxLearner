using System.Security.Claims;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Keycloak.AuthServices.Sdk.Admin.Requests.Users;
using LinuxLearner.Database;
using LinuxLearner.Domain;
using LinuxLearner.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class IntegrationTest : IClassFixture<IntegrationTestFactory>
{
    protected readonly IServiceProvider Services;
    protected AppDbContext DbContext => Services.GetRequiredService<AppDbContext>();
    protected IKeycloakUserClient KeycloakUserClient => Services.GetRequiredService<IKeycloakUserClient>();

    private static bool _migrationComplete;
    
    protected IntegrationTest(IntegrationTestFactory factory)
    {
        Services = factory.Services.CreateScope().ServiceProvider;

        if (_migrationComplete) return;
        DbContext.Database.Migrate();
        _migrationComplete = true;
    }

    protected async Task<Guid> CreateKeycloakUserAsync(User baseUser)
    {
        return await CreateKeycloakUserAsync(baseUser.UserType);
    }

    protected async Task<Guid> CreateKeycloakUserAsync(UserType userType = UserType.Student)
    {
        var username = "test-user-" + Guid.NewGuid();
        await KeycloakUserClient.CreateUserAsync("master", new UserRepresentation
        {
            Username = username,
            Enabled = true
        });

        var userService = Services.GetRequiredService<UserService>();

        var matchingUsers = await KeycloakUserClient.GetUsersAsync("master", new GetUsersRequestParameters
        {
            Username = username,
            Exact = true,
            Max = 1,
            BriefRepresentation = true
        });
        var keycloakUserId = matchingUsers.First().Id!;
        
        var studentsId = await userService.GetKeycloakGroupId(UserType.Student);
        var teachersId = await userService.GetKeycloakGroupId(UserType.Teacher);
        var adminsId = await userService.GetKeycloakGroupId(UserType.Admin);

        await KeycloakUserClient.JoinGroupAsync("master", keycloakUserId, studentsId);

        if (userType != UserType.Student)
        {
            await KeycloakUserClient.JoinGroupAsync("master", keycloakUserId, teachersId);
        }

        if (userType == UserType.Admin)
        {
            await KeycloakUserClient.JoinGroupAsync("master", keycloakUserId, adminsId);
        }

        return Guid.Parse(keycloakUserId);
    }

    protected async Task<DefaultHttpContext> MakeContextAndUserAsync(UserType userType)
    {
        var keycloakUserId = await CreateKeycloakUserAsync(userType);
        var httpContext = MakeContext(userType, keycloakUserId);
        return httpContext;
    }

    protected static DefaultHttpContext MakeContext(User baseUser) =>
        MakeContext(baseUser.UserType, baseUser.Id);
    
    protected static DefaultHttpContext MakeContext(UserType userType, Guid? userId = null)
    {
        return userType switch
        {
            UserType.Student => MakeContext("student", userId),
            UserType.Teacher => MakeContext("teacher", userId),
            UserType.Admin => MakeContext("admin", userId),
            _ => throw new ArgumentOutOfRangeException(nameof(userType), userType, null)
        };
    }

    protected static Guid GetUserIdFromContext(HttpContext httpContext)
    {
        var stringValue = httpContext.User.Claims.First(c => c.Type.EndsWith("nameidentifier")).Value;
        return Guid.Parse(stringValue);
    }
    
    private static DefaultHttpContext MakeContext(string role, Guid? userId = null)
    {
        userId ??= Guid.NewGuid(); // fake the user ID to avoid creating actual Keycloak accounts when not necessary
        var httpContext = new DefaultHttpContext();
        
        var claimsPrincipal = new ClaimsPrincipal();
        var identity = new ClaimsIdentity(
        [
            new Claim("preferred_username", role + Random.Shared.Next(100000, 1000000)),
            new Claim("resource_access", role),
            new Claim("nameidentifier", userId.ToString()!)
        ], "Bearer", "preferred_username", "resource_access");

        claimsPrincipal.AddIdentity(identity);

        httpContext.User = claimsPrincipal;

        return httpContext;
    }
}