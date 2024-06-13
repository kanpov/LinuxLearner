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
        studentApi.MapPost("/courses/{courseId:guid}/join/without-invite", JoinCourseWithoutInvite);
        studentApi.MapPost("/courses/{courseId:guid}/leave", LeaveCourse);
        
        teacherApi.MapPost("/courses/{courseId:guid}/invites", CreateInvite);
        teacherApi.MapPatch("/courses/{courseId:guid}/invites/{inviteId:guid}", PatchInvite);
        teacherApi.MapDelete("/courses/{courseId:guid}/invites/{inviteId:guid}", DeleteInvite);
        teacherApi.MapGet("/courses/{courseId:guid}/invites", GetInvitesForCourse);
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
        var inviteDto = await inviteService.GetInviteAsync(inviteId);
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

    private static async Task<Results<NotFound, NoContent>> DeleteInvite(
        CourseInviteService courseInviteService, HttpContext httpContext, Guid courseId, Guid inviteId)
    {
        var success = await courseInviteService.DeleteInviteAsync(httpContext, courseId, inviteId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, Ok<IEnumerable<CourseInviteDto>>>> GetInvitesForCourse(
        CourseInviteService courseInviteService, HttpContext httpContext, Guid courseId)
    {
        var inviteDtos = await courseInviteService.GetInvitesForCourseAsync(httpContext, courseId);
        return inviteDtos is null ? TypedResults.NotFound() : TypedResults.Ok(inviteDtos);
    }

    private static async Task<Results<NotFound, NoContent>> JoinCourseWithoutInvite(
        CourseInviteService courseInviteService, HttpContext httpContext, Guid courseId)
    {
        var success = await courseInviteService.JoinCourseWithoutInviteAsync(httpContext, courseId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<ForbidHttpResult, NoContent>> LeaveCourse(
        CourseInviteService courseInviteService, HttpContext httpContext, Guid courseId)
    {
        var success = await courseInviteService.LeaveCourseAsync(httpContext, courseId);
        return success ? TypedResults.NoContent() : TypedResults.Forbid();
    }
}