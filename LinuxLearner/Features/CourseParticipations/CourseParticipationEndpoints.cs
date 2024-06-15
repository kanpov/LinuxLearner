using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.CourseParticipations;

public static class CourseParticipationEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi)
    {
        studentApi.MapGet("/participations/course/{courseId:guid}/user/{userId:guid}", GetParticipation);
        studentApi.MapGet("/participations/course/{courseId:guid}", GetParticipationsForCourse);
        studentApi.MapGet("/participations/user/{userId:guid}", GetParticipationsForUser);

        teacherApi.MapDelete("/participations/course/{courseId:guid}/user/{userId:guid}", DeleteParticipation);
        teacherApi.MapPost("/participations/course/{courseId:guid}/user/{userId:guid}/administration/grant", GrantCourseAdministration);
        teacherApi.MapPost("/participations/course/{courseId:guid}/user/{userId:guid}/administration/revoke", RevokeCourseAdministration);
    }
    
    private static async Task<Results<NotFound, Ok<CourseParticipationDto>>> GetParticipation(
        CourseParticipationService courseParticipationService, Guid courseId, Guid userId)
    {
        var courseUserDto = await courseParticipationService.GetParticipationAsync(courseId, userId);
        return courseUserDto is null ? TypedResults.NotFound() : TypedResults.Ok(courseUserDto);
    }

    private static async Task<Results<NotFound, Ok<IEnumerable<CourseParticipationWithoutCourseDto>>>> GetParticipationsForCourse(
        CourseParticipationService courseParticipationService, HttpContext httpContext, Guid courseId)
    {
        var courseParticipationDtos = await courseParticipationService.GetParticipationsForCourseAsync(httpContext, courseId);
        return courseParticipationDtos is null ? TypedResults.NotFound() : TypedResults.Ok(courseParticipationDtos);
    }
    
    private static async Task<Ok<IEnumerable<CourseParticipationWithoutUserDto>>> GetParticipationsForUser(
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

    private static async Task<Results<NotFound, NoContent>> DeleteParticipation(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid userId, Guid courseId)
    {
        var success = await courseParticipationService.DeleteParticipationAsync(httpContext, courseId, userId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}