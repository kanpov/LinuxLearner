using FluentValidation;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.CourseInvites;

public static class CourseInviteEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/courses/{courseId:guid}/invites/{inviteId:guid}", GetInvite)
            .WithName(nameof(GetInvite));
        
        teacherApi.MapPost("/courses/{courseId:guid}/invites", CreateInvite);
        teacherApi.MapPatch("/courses/{courseId:guid}/invites/{inviteId:guid}", PatchInvite);
    }

    private static async Task<Results<ValidationProblem, NotFound, CreatedAtRoute<CourseInviteDto>>> CreateInvite(
        CourseInviteService inviteService, CourseInviteCreateDto inviteCreateDto, Guid courseId,
        IValidator<CourseInviteCreateDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(inviteCreateDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var inviteDto = await inviteService.CreateInviteAsync(httpContext, courseId, inviteCreateDto);
        if (inviteDto is null) return TypedResults.NotFound();
        return TypedResults.CreatedAtRoute(inviteDto, nameof(GetInvite),
            new { inviteId = inviteDto.Id, courseId = inviteDto.Course.Id });
    }

    private static async Task<Results<NotFound, Ok<CourseInviteDto>>> GetInvite(
        CourseInviteService inviteService, Guid courseId, Guid inviteId)
    {
        var inviteDto = await inviteService.GetInviteAsync(courseId, inviteId);
        return inviteDto is null ? TypedResults.NotFound() : TypedResults.Ok(inviteDto);
    }

    private static async Task<Results<ValidationProblem, NotFound, NoContent>> PatchInvite(
        CourseInviteService courseInviteService, HttpContext httpContext, Guid courseId, Guid inviteId,
        IValidator<CourseInvitePatchDto> validator, CourseInvitePatchDto invitePatchDto)
    {
        var validationResult = await validator.ValidateAsync(invitePatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var success = await courseInviteService.PatchInviteAsync(httpContext, courseId, inviteId, invitePatchDto);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}