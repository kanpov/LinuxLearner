using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Users;

public static class UserEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/user/self", GetSelfUser);
        studentApi.MapDelete("/user/self", DeleteSelfUser);
        studentApi.MapGet("/users/{userId:guid}", GetUser);
        
        teacherApi.MapPut("/users/{userId:guid}/promote", PromoteUser);
        teacherApi.MapPut("/users/{userId:guid}/demote", DemoteUser);
        
        adminApi.MapDelete("/users/{userId:guid}", DeleteUser);
    }

    private static async Task<Ok<UserDto>> GetSelfUser(HttpContext httpContext, UserService userService)
    {
        var userDto = await userService.GetAuthorizedUserAsync(httpContext);
        return TypedResults.Ok(userDto);
    }

    private static async Task<NoContent> DeleteSelfUser(UserService userService, HttpContext httpContext)
    {
        await userService.DeleteAuthorizedUserAsync(httpContext);
        return TypedResults.NoContent();
    }
    
    private static async Task<Results<NotFound, NoContent>> DeleteUser(UserService userService, Guid userId)
    {
        var successful = await userService.DeleteUserAsync(userId);
        return successful ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, Ok<UserDto>>> GetUser(UserService userService, Guid userId)
    {
        var userDto = await userService.GetUserAsync(userId);
        if (userDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(userDto);
    }
    private static async Task<Results<ForbidHttpResult, NoContent>> PromoteUser(HttpContext httpContext,
        UserService userService, Guid userId)
    {
        var successful = await userService.ChangeUserRoleAsync(httpContext, userId, demote: false);
        return successful ? TypedResults.NoContent() : TypedResults.Forbid();
    }
    
    private static async Task<Results<ForbidHttpResult, NoContent>> DemoteUser(HttpContext httpContext,
        UserService userService, Guid userId)
    {
        var successful = await userService.ChangeUserRoleAsync(httpContext, userId, demote: true);
        return successful ? TypedResults.NoContent() : TypedResults.Forbid();
    }
}