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

    private IKeycloakUserClient KeycloakUserClient => Services.GetRequiredService<IKeycloakUserClient>();

    private static bool _migrationComplete;
    
    protected IntegrationTest(IntegrationTestFactory factory)
    {
        Services = factory.Services.CreateScope().ServiceProvider;

        if (_migrationComplete) return;
        DbContext.Database.Migrate();
        _migrationComplete = true;
    }

    protected async Task InitializeKeycloakUserAsync(User baseUser)
    {
        await KeycloakUserClient.CreateUserAsync("master", new UserRepresentation
        {
            Username = baseUser.Name,
            Enabled = true
        });

        var userService = Services.GetRequiredService<UserService>();
        
        var userId = await userService.GetKeycloakUserId(baseUser.Name);
        
        var studentsId = await userService.GetKeycloakGroupId(UserType.Student);
        var teachersId = await userService.GetKeycloakGroupId(UserType.Teacher);
        var adminsId = await userService.GetKeycloakGroupId(UserType.Admin);

        await KeycloakUserClient.JoinGroupAsync("master", userId, studentsId);

        if (baseUser.UserType != UserType.Student)
        {
            await KeycloakUserClient.JoinGroupAsync("master", userId, teachersId);
        }

        if (baseUser.UserType == UserType.Admin)
        {
            await KeycloakUserClient.JoinGroupAsync("master", userId, adminsId);
        }
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