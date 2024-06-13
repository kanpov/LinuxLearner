using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.CourseParticipations;

public static class CourseParticipationEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/participations/course/{courseId:guid}/user/{userId:guid}", GetParticipation);
        studentApi.MapGet("/participations/course/{courseId:guid}", GetParticipationsForCourse);
        studentApi.MapGet("/participations/user/{userId:guid}", GetParticipationsForUser);
        studentApi.MapDelete("/participations/course/{courseId:guid}/user/self", DeleteOwnParticipation);

        teacherApi.MapDelete("/participations/course/{courseId:guid}/user/{userId:guid}", DeleteParticipation);
        
        adminApi.MapPost("/participations/course/{courseId:guid}/user/{userId:guid}/administration/grant", GrantCourseAdministration);
        adminApi.MapPost("/participations/course/{courseId:guid}/user/{userId:guid}/administration/revoke", RevokeCourseAdministration);
        adminApi.MapDelete("/participations/course/{courseId:guid}/user/{userId:guid}/force", ForceDeleteParticipation);
    }
    
    private static async Task<Results<NotFound, Ok<CourseParticipationDto>>> GetParticipation(
        CourseParticipationService courseParticipationService, Guid courseId, Guid userId)
    {
        var courseUserDto = await courseParticipationService.GetParticipationAsync(courseId, userId);
        return courseUserDto is null ? TypedResults.NotFound() : TypedResults.Ok(courseUserDto);
    }

    private static async Task<Results<NotFound, Ok<IEnumerable<CourseParticipationDto>>>> GetParticipationsForCourse(
        CourseParticipationService courseParticipationService, HttpContext httpContext, Guid courseId)
    {
        var courseParticipationDtos = await courseParticipationService.GetParticipationsForCourseAsync(httpContext, courseId);
        return courseParticipationDtos is null ? TypedResults.NotFound() : TypedResults.Ok(courseParticipationDtos);
    }
    
    private static async Task<Ok<IEnumerable<CourseParticipationDto>>> GetParticipationsForUser(
        CourseParticipationService courseParticipationService, Guid userId)
    {
        var courseParticipationDtos = await courseParticipationService.GetParticipationsForUserAsync(userId);
        return TypedResults.Ok(courseParticipationDtos);
    }
    
    private static async Task<Results<NotFound, NoContent>> GrantCourseAdministration(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid courseId, Guid userId)
    {
        var success = await courseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, courseId, userId,
                isCourseAdministrator: true);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
    
    private static async Task<Results<NotFound, NoContent>> RevokeCourseAdministration(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid courseId, Guid userId)
    {
        var success = await courseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, courseId,
            userId, isCourseAdministrator: false);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, NoContent>> DeleteOwnParticipation(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid courseId)
    {
        var success = await courseParticipationService.DeleteOwnParticipationAsync(httpContext, courseId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, NoContent>> DeleteParticipation(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid userId, Guid courseId)
    {
        var success = await courseParticipationService.DeleteParticipationAsync(httpContext, courseId, userId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, NoContent>> ForceDeleteParticipation(
        CourseParticipationService courseParticipationService, Guid userId, Guid courseId)
    {
        var success = await courseParticipationService.ForceDeleteParticipationAsync(courseId, userId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}