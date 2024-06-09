using System.Security.Claims;
using LinuxLearner.Database;
using LinuxLearner.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace LinuxLearner.IntegrationTests;

public class IntegrationTest : IClassFixture<IntegrationTestFactory>
{
    protected readonly IServiceProvider Services;
    protected AppDbContext DbContext => Services.GetRequiredService<AppDbContext>();
    protected IFusionCache FusionCache => Services.GetRequiredService<IFusionCache>();

    private static bool _migrationComplete;
    
    protected IntegrationTest(IntegrationTestFactory factory)
    {
        Services = factory.Services.CreateScope().ServiceProvider;

        if (_migrationComplete) return;
        DbContext.Database.Migrate();
        _migrationComplete = true;
    }

    protected static HttpContext MakeContext(UserType userType)
    {
        return userType switch
        {
            UserType.Student => MakeContext("student"),
            UserType.Teacher => MakeContext("teacher"),
            UserType.Admin => MakeContext("admin"),
            _ => throw new ArgumentOutOfRangeException(nameof(userType), userType, null)
        };
    }
    
    private static HttpContext MakeContext(string role)
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