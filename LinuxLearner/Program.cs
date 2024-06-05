using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/student", () => "Student!").RequireAuthorization("student");
app.MapGet("/teacher", () => "Teacher!").RequireAuthorization("teacher");

app.Run();
