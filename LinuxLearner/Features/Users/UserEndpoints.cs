using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/self", GetSelfUser).RequireAuthorization();
        builder.MapGet("/users/{username}", GetUser).RequireAuthorization();
    }

    private static async Task<Ok<UserDto>> GetSelfUser(ClaimsPrincipal claimsPrincipal, UserService userService)
    {
        var userDto = await userService.GetAuthorizedUserAsync(claimsPrincipal);
        return TypedResults.Ok(userDto);
    }

    private static async Task<Results<NotFound, Ok<UserDto>>> GetUser(UserService userService, string username)
    {
        var userDto = await userService.GetUserAsync(username);
        if (userDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(userDto);
    }
}