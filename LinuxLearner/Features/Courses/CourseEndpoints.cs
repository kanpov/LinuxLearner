using FluentValidation;
using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
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

        adminApi.MapPatch("/courses/{courseId:guid}/force", ForcePatchCourse);
        adminApi.MapDelete("/courses/{courseId:guid}/force", ForceDeleteCourse);
    }

    private static async Task<Ok<IEnumerable<CourseDto>>> GetCourses(
        CourseService courseService, int page, int pageSize = 10, string? name = null, string? description = null,
        AcceptanceMode? acceptanceMode = null, CourseSortParameter sortParameter = CourseSortParameter.Name)
    {
        return TypedResults.Ok(await courseService.GetCoursesAsync(page, pageSize, name, description, acceptanceMode, sortParameter));
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

    private static async Task<Results<NotFound, NoContent>> ForceDeleteCourse(CourseService courseService, Guid courseId)
    {
        var success = await courseService.ForceDeleteCourseAsync(courseId);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<ValidationProblem, NotFound, NoContent>> ForcePatchCourse(
        CourseService courseService, Guid courseId, CoursePatchDto coursePatchDto,
        IValidator<CoursePatchDto> validator, HttpContext httpContext)
    {
        var validationResult = await validator.ValidateAsync(coursePatchDto);
        if (!validationResult.IsValid) return validationResult.ToProblem(httpContext);

        var success = await courseService.ForcePatchCourseAsync(courseId, coursePatchDto);
        return success ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}