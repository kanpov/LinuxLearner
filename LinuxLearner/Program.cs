using System.Text.Json.Serialization;
using FluentValidation;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Keycloak.AuthServices.Sdk;
using LinuxLearner.Database;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Courses;
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
    
    options.AddPolicy("admin", policy =>
    {
        policy.RequireResourceRoles("admin");
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
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<CourseParticipationRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CourseService>();
builder.Services.AddScoped<CourseParticipationService>();

builder.Services.AddScoped<IValidator<CourseCreateDto>, CourseCreateDtoValidator>();
builder.Services.AddScoped<IValidator<CoursePatchDto>, CoursePatchDtoValidator>();
// serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// admin API
builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(
        "admin-api",
        client =>
        {
            var options = builder.Configuration.GetKeycloakOptions<KeycloakAdminClientOptions>("KeycloakAdmin")!;
            client.ClientId = options.Resource;
            client.ClientSecret = options.Credentials.Secret;
            client.TokenEndpoint = options.KeycloakTokenEndpoint;
        });
builder.Services
    .AddKeycloakAdminHttpClient(builder.Configuration.GetRequiredSection("KeycloakAdmin"))
    .AddClientCredentialsTokenHandler("admin-api");

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

var studentApi = app.MapGroup("/api/student/v1").RequireAuthorization("student");
var teacherApi = app.MapGroup("/api/teacher/v1").RequireAuthorization("teacher");
var adminApi = app.MapGroup("/api/admin/v1").RequireAuthorization("admin");

UserEndpoints.Map(studentApi, teacherApi, adminApi);
CourseEndpoints.Map(studentApi, teacherApi, adminApi);
CourseParticipationEndpoints.Map(studentApi, teacherApi, adminApi);

app.Run();


public partial class Program;
