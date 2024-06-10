using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.CourseParticipations;

public static class CourseParticipationEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/participations/course/{id:guid}/user/{username}", GetParticipation);
        studentApi.MapGet("/participations/course/{id:guid}", GetParticipationsForCourse);
        studentApi.MapGet("/participations/user/{username}", GetParticipationsForUser);
        
        adminApi.MapPut("/participations/course/{id:guid}/user/{username}/administration/grant", GrantCourseAdministration);
        adminApi.MapPut("/participations/course/{id:guid}/user/{username}/administration/revoke", RevokeCourseAdministration);
    }
    
    private static async Task<Results<NotFound, Ok<CourseParticipationDto>>> GetParticipation(
        CourseParticipationService courseParticipationService, Guid id, string username)
    {
        var courseUserDto = await courseParticipationService.GetParticipationAsync(id, username);
        return courseUserDto is null ? TypedResults.NotFound() : TypedResults.Ok(courseUserDto);
    }

    private static async Task<Results<NotFound, Ok<IEnumerable<CourseParticipationDto>>>> GetParticipationsForCourse(
        CourseParticipationService courseParticipationService, HttpContext httpContext, Guid id)
    {
        var courseParticipationDtos = await courseParticipationService.GetParticipationsForCourseAsync(httpContext, id);
        return courseParticipationDtos is null ? TypedResults.NotFound() : TypedResults.Ok(courseParticipationDtos);
    }
    
    private static async Task<Ok<IEnumerable<CourseParticipationDto>>> GetParticipationsForUser(
        CourseParticipationService courseParticipationService, string username)
    {
        var courseParticipationDtos = await courseParticipationService.GetParticipationsForUserAsync(username);
        return TypedResults.Ok(courseParticipationDtos);
    }
    
    private static async Task<Results<NotFound, NoContent>> GrantCourseAdministration(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid id, string username)
    {
        var success = await courseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, id, username,
                isCourseAdministrator: true);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
    
    private static async Task<Results<NotFound, NoContent>> RevokeCourseAdministration(
        HttpContext httpContext, CourseParticipationService courseParticipationService, Guid id, string username)
    {
        var success = await courseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, id,
            username, isCourseAdministrator: false);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}