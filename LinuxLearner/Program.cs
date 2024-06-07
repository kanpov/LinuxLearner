using System.Text.Json.Serialization;
using FluentValidation;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using LinuxLearner.Database;
using LinuxLearner.Features.Users;
using LinuxLearner.Utilities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

// logging
builder.Services.AddSerilog(config =>
{
    config.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);
    config.MinimumLevel.Information();
    config.Enrich.FromLogContext();
    const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message} {ClassName} \n";
    config.WriteTo.Console(outputTemplate: template);
});
// swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<EnumSchemaFilter>();
});
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
builder.Services.AddFusionCacheSystemTextJsonSerializer();
// DB
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
});
// DI
builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<IValidator<UserPatchDto>, UserPatchDtoValidator>();
// serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

var group = app.MapGroup("/api/v1").RequireAuthorization();

group.MapUserEndpoints();

app.Run();
