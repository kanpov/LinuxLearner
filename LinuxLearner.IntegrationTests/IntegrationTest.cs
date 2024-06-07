using LinuxLearner.Database;
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

        // if (_migrationComplete) return;
        // DbContext.Database.Migrate();
        // _migrationComplete = true;
    }
}