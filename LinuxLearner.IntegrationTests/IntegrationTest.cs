using System.Security.Claims;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
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

    protected async Task<string> CreateKeycloakUserAsync(User baseUser)
    {
        return await CreateKeycloakUserAsync(baseUser.Name, baseUser.UserType);
    }

    protected async Task<string> CreateKeycloakUserAsync(string username, UserType userType = UserType.Student)
    {
        await KeycloakUserClient.CreateUserAsync("master", new UserRepresentation
        {
            Username = username,
            Enabled = true
        });

        var userService = Services.GetRequiredService<UserService>();
        
        var keycloakUserId = await userService.GetKeycloakUserId(username);
        
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

        return keycloakUserId;
    }

    protected static DefaultHttpContext MakeContext(UserType userType)
    {
        return userType switch
        {
            UserType.Student => MakeContext("student"),
            UserType.Teacher => MakeContext("teacher"),
            UserType.Admin => MakeContext("admin"),
            _ => throw new ArgumentOutOfRangeException(nameof(userType), userType, null)
        };
    }
    
    private static DefaultHttpContext MakeContext(string role)
    {
        var httpContext = new DefaultHttpContext();
        
        var claimsPrincipal = new ClaimsPrincipal();
        claimsPrincipal.AddIdentity(new ClaimsIdentity(
        [
            new Claim("preferred_username", role + Random.Shared.Next(100000, 1000000)),
            new Claim("resource_access", role)
        ], "Bearer", "preferred_username", "resource_access"));

        httpContext.User = claimsPrincipal;

        return httpContext;
    }
}