using FluentValidation;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Users;

public static class UserEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/user/self", GetSelfUser);
        studentApi.MapPatch("/user/self", PatchSelfUser);
        studentApi.MapDelete("/user/self", DeleteSelfUser);
        studentApi.MapGet("/users/{username}", GetUser);
        studentApi.MapGet("/users", GetUsers);
        
        teacherApi.MapPut("/users/{username}/promote", PromoteUser);
        teacherApi.MapPut("/users/{username}/demote", DemoteUser);

        adminApi.MapPatch("/users/{username}", PatchUser);
        adminApi.MapDelete("/users/{username}", DeleteUser);
    }

    private static async Task<Ok<UserDto>> GetSelfUser(HttpContext httpContext, UserService userService)
    {
        var userDto = await userService.GetAuthorizedUserAsync(httpContext);
        return TypedResults.Ok(userDto);
    }
    
    private static async Task<Results<ValidationProblem, Ok<UserDto>>> PatchSelfUser(
        UserService userService, HttpContext httpContext, UserPatchDto userPatchDto,
        IValidator<UserPatchDto> validator)
    {
        var validationResult = await validator.ValidateAsync(userPatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);
        
        var userDto = await userService.PatchAuthorizedUserAsync(httpContext, userPatchDto);
        return TypedResults.Ok(userDto);
    }

    private static async Task<Results<ValidationProblem, NotFound, Ok<UserDto>>> PatchUser(
        UserService userService, HttpContext httpContext, string username,
        UserPatchDto userPatchDto, IValidator<UserPatchDto> validator)
    {
        var validationResult = await validator.ValidateAsync(userPatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var userDto = await userService.PatchUserAsync(username, userPatchDto);
        return userDto is null ? TypedResults.NotFound() : TypedResults.Ok(userDto);
    }

    private static async Task<NoContent> DeleteSelfUser(UserService userService, HttpContext httpContext)
    {
        await userService.DeleteAuthorizedUserAsync(httpContext);
        return TypedResults.NoContent();
    }
    
    private static async Task<Results<NotFound, NoContent>> DeleteUser(UserService userService, string username)
    {
        var successful = await userService.DeleteUserAsync(username);
        return successful ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, Ok<UserDto>>> GetUser(UserService userService, string username)
    {
        var userDto = await userService.GetUserAsync(username);
        if (userDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(userDto);
    }

    private static async Task<Ok<IEnumerable<UserDto>>> GetUsers(
        UserService userService, int page = 1, int pageSize = 5)
    {
        var userDtos = await userService.GetUsersAsync(page, pageSize);
        return TypedResults.Ok(userDtos);
    }

    private static async Task<Results<ForbidHttpResult, NoContent>> PromoteUser(HttpContext httpContext,
        UserService userService, string username)
    {
        var successful = await userService.ChangeUserRoleAsync(httpContext, username, demote: false);
        return successful ? TypedResults.NoContent() : TypedResults.Forbid();
    }
    
    private static async Task<Results<ForbidHttpResult, NoContent>> DemoteUser(HttpContext httpContext,
        UserService userService, string username)
    {
        var successful = await userService.ChangeUserRoleAsync(httpContext, username, demote: true);
        return successful ? TypedResults.NoContent() : TypedResults.Forbid();
    }
}