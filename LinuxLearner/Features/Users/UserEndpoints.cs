using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/users/self", GetSelfUser).RequireAuthorization();
    }

    private static async Task<Ok<UserDto>> GetSelfUser(ClaimsPrincipal claimsPrincipal, UserService userService)
    {
        return TypedResults.Ok(await userService.GetUserAsync(claimsPrincipal));
    }
}