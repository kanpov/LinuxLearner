using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using LinuxLearner.Database;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// auth
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("student", policy =>
    {
        policy.RequireResourceRoles("student");
    });
    
    options.AddPolicy("teacher", policy =>
    {
        policy.RequireResourceRoles("teacher");
    });
}).AddKeycloakAuthorization(builder.Configuration);
// caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("KeyDB");
});
builder.Services.AddMemoryCache();
builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache();
builder.Services.AddFusionCacheCysharpMemoryPackSerializer();
// DB
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
});

var app = builder.Build();

// swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// auth
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", async (IFusionCache fusionCache) =>
{
    var i = await fusionCache.GetOrSetAsync("my-key", 1);
    return i.ToString();
});

app.Run();
