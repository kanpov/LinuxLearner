using FluentValidation;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/user/self", GetSelfUser);
        builder.MapPatch("/user/self", PatchSelfUser);
        builder.MapDelete("/user/self", DeleteSelfUser);
        
        builder.MapGet("/users/{username}", GetUser);
        builder.MapGet("/users", GetUsers);
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

    private static async Task<NoContent> DeleteSelfUser(UserService userService, HttpContext httpContext)
    {
        await userService.DeleteAuthorizedUserAsync(httpContext);
        return TypedResults.NoContent();
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


}