using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Courses;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/courses", CreateCourse)
            .RequireAuthorization("teacher");

        builder.MapGet("/courses/{id:guid}", GetCourse)
            .WithName(nameof(GetCourse));
    }

    private static async Task<Results<ForbidHttpResult, CreatedAtRoute<CourseDto>>> CreateCourse(
        CourseService courseService, CourseCreateDto courseCreateDto)
    {
        var courseDto = await courseService.CreateCourseAsync(courseCreateDto);
        return TypedResults.CreatedAtRoute(courseDto, nameof(GetCourse), new { id = courseDto.Id });
    }

    private static async Task<Results<NotFound, Ok<CourseDto>>> GetCourse(
        CourseService courseService, Guid id)
    {
        var courseDto = await courseService.GetCourseAsync(id);
        if (courseDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(courseDto);
    }
}