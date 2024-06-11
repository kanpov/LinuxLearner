using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using LinuxLearner.Database;
using LinuxLearner.Features.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LinuxLearner.IntegrationTests;

public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly PostgreSqlContainer PostgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("linuxlearner")
        .WithUsername("linuxlearner")
        .WithPassword("linuxlearner")
        .WithCleanUp(true)
        .Build();

    private const int KeyDbInternalPort = 6379;
    private static readonly IContainer KeyDbContainer = new ContainerBuilder()
        .WithImage("eqalpha/keydb")
        .WithCommand("keydb-server")
        .WithPortBinding(hostPort: Random.Shared.Next(10000, 65536), containerPort: KeyDbInternalPort)
        .WithCleanUp(true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var postgresOptions = services
                .FirstOrDefault(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (postgresOptions is not null)
            {
                services.Remove(postgresOptions);
            }

            var cacheOptions = services
                .FirstOrDefault(s => s.ServiceType == typeof(RedisCacheOptions));
            if (cacheOptions is not null)
            {
                services.Remove(cacheOptions);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(PostgresContainer.GetConnectionString(), npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure();
                });
            });

            services.AddStackExchangeRedisCache(options =>
            {
                var keyDbExternalPort = KeyDbContainer.GetMappedPublicPort(KeyDbInternalPort);
                options.Configuration = $"localhost:{keyDbExternalPort}";
            });
        });
    }

    public async Task InitializeAsync()
    {
        UserService.KeycloakAvailable = false;
        
        var tasks = new List<Task>();
        
        if (PostgresContainer.State != TestcontainersStates.Running)
        {
            tasks.Add(PostgresContainer.StartAsync());
        }

        if (KeyDbContainer.State != TestcontainersStates.Running)
        {
            tasks.Add(KeyDbContainer.StartAsync());
        }

        await Task.WhenAll(tasks);
    }

    public new async Task DisposeAsync()
    {
        await PostgresContainer.StopAsync();
        await KeyDbContainer.StopAsync();
    }
}