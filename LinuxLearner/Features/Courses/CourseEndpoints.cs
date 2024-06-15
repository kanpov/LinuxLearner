using FluentValidation;
using LinuxLearner.Domain;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Features.Courses;

public static class CourseEndpoints
{
    public static void Map(RouteGroupBuilder studentApi, RouteGroupBuilder teacherApi, RouteGroupBuilder adminApi)
    {
        studentApi.MapGet("/courses/{courseId:guid}", GetCourse).WithName(nameof(GetCourse));
        studentApi.MapGet("/courses", GetCourses);
        
        teacherApi.MapPost("/courses", CreateCourse);
        teacherApi.MapPatch("/courses/{courseId:guid}", PatchCourse);
        teacherApi.MapDelete("/courses/{courseId:guid}", DeleteCourse);
    }

    private static async Task<Ok<IEnumerable<CourseDto>>> GetCourses(
        CourseService courseService, int page, HttpContext httpContext,
        int pageSize = 10, string? name = null, string? description = null,
        AcceptanceMode? acceptanceMode = null, string? search = null,
        CourseSortParameter sortParameter = CourseSortParameter.Name)
    {
        var (totalAmount, courseDtos) =
            await courseService.GetCoursesAsync(httpContext, page, pageSize, name, description, acceptanceMode, search, sortParameter);
        PaginationData.Add(httpContext, totalAmount, page, pageSize);
        
        return TypedResults.Ok(courseDtos);
    }

    private static async Task<Results<ValidationProblem, CreatedAtRoute<CourseDto>>> CreateCourse(
        CourseService courseService, CourseCreateDto courseCreateDto,
        IValidator<CourseCreateDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(courseCreateDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var courseDto = await courseService.CreateCourseAsync(httpContext, courseCreateDto);
        return TypedResults.CreatedAtRoute(courseDto, nameof(GetCourse), new { courseId = courseDto.Id });
    }

    private static async Task<Results<ValidationProblem, NotFound, NoContent>> PatchCourse(
        CourseService courseService, Guid courseId, CoursePatchDto coursePatchDto,
        IValidator<CoursePatchDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(coursePatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var success = await courseService.PatchCourseAsync(httpContext, courseId, coursePatchDto);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, NoContent>> DeleteCourse(CourseService courseService, Guid courseId,
        HttpContext httpContext)
    {
        var success = await courseService.DeleteCourseAsync(httpContext, courseId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NotFound, Ok<CourseDto>>> GetCourse(
        CourseService courseService, Guid courseId)
    {
        var courseDto = await courseService.GetCourseAsync(courseId);
        if (courseDto is null) return TypedResults.NotFound();
        return TypedResults.Ok(courseDto);
    }
}