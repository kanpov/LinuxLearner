using FluentValidation;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Courses;

public static class CourseEndpoints
{
    public static void MapCourseEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/courses", CreateCourse)
            .RequireAuthorization("teacher");

        builder.MapPatch("/courses/{id:guid}", PatchCourse)
            .RequireAuthorization("teacher");

        builder.MapDelete("/courses/{id:guid}", DeleteCourse)
            .RequireAuthorization("teacher");

        builder.MapGet("/courses/{id:guid}", GetCourse)
            .WithName(nameof(GetCourse));
    }

    private static async Task<Results<ValidationProblem, CreatedAtRoute<CourseDto>>> CreateCourse(
        CourseService courseService, CourseCreateDto courseCreateDto,
        IValidator<CourseCreateDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(courseCreateDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var courseDto = await courseService.CreateCourseAsync(httpContext, courseCreateDto);
        return TypedResults.CreatedAtRoute(courseDto, nameof(GetCourse), new { id = courseDto.Id });
    }

    private static async Task<Results<ValidationProblem, NotFound, NoContent>> PatchCourse(
        CourseService courseService, Guid id, CoursePatchDto coursePatchDto,
        IValidator<CoursePatchDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(coursePatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var success = await courseService.PatchCourseAsync(httpContext, id, coursePatchDto);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, NoContent>> DeleteCourse(CourseService courseService, Guid id,
        HttpContext httpContext)
    {
        var success = await courseService.DeleteCourseAsync(httpContext, id);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, Ok<CourseDto>>> GetCourse(
        CourseService courseService, Guid id)
    {
        var courseDto = await courseService.GetCourseAsync(id);
        if (courseDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(courseDto);
    }
}