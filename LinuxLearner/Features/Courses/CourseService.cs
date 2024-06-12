using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.Courses;

public class CourseService(
    CourseRepository courseRepository,
    UserService userService,
    CourseParticipationService courseParticipationService)
{
    public async Task<CourseDto> CreateCourseAsync(HttpContext httpContext, CourseCreateDto courseCreateDto)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var course = MapToCourse(courseCreateDto);
        await courseRepository.AddCourseAsync(course);

        await courseParticipationService.CreateParticipationAsync(new CourseParticipation
        {
            CourseId = course.Id,
            UserId = user.Id,
            IsCourseAdministrator = true,
            JoinTime = DateTimeOffset.UtcNow
        });
        
        return MapToCourseDto(course);
    }

    public async Task<CourseDto?> GetCourseAsync(Guid courseId)
    {
        var course = await courseRepository.GetCourseAsync(courseId);
        return course is null ? null : MapToCourseDto(course);
    }

    public async Task<bool> PatchCourseAsync(HttpContext httpContext, Guid courseId, CoursePatchDto coursePatchDto)
    {
        var courseUser = await courseParticipationService.GetAdministrativeParticipationAsync(httpContext, courseId);
        if (courseUser is null) return false;
        
        ProjectCoursePatchDto(courseUser.Course, coursePatchDto);
        await courseRepository.UpdateCourseAsync(courseUser.Course);
        return true;
    }

    public async Task<bool> ForcePatchCourseAsync(Guid courseId, CoursePatchDto coursePatchDto)
    {
        var course = await courseRepository.GetCourseAsync(courseId);
        if (course is null) return false;
        
        ProjectCoursePatchDto(course, coursePatchDto);
        await courseRepository.UpdateCourseAsync(course);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var courseUser = await courseParticipationService.GetAdministrativeParticipationAsync(httpContext, courseId);
        if (courseUser is null) return false;

        await courseRepository.DeleteCourseAsync(courseId);
        return true;
    }

    public async Task<bool> ForceDeleteCourseAsync(Guid courseId)
    {
        var course = await courseRepository.GetCourseAsync(courseId);
        if (course is null) return false;

        await courseRepository.DeleteCourseAsync(courseId);
        return true;
    }
    
    public static CourseDto MapToCourseDto(Course course) =>
        new(course.Id, course.Name, course.Description, course.AcceptanceMode);

    private static Course MapToCourse(CourseCreateDto courseCreateDto) => new()
    {
        Name = courseCreateDto.Name,
        Description = courseCreateDto.Description,
        AcceptanceMode = courseCreateDto.AcceptanceMode
    };

    private static void ProjectCoursePatchDto(Course course, CoursePatchDto coursePatchDto)
    {
        if (coursePatchDto.Name is not null) course.Name = coursePatchDto.Name;
        course.Description = coursePatchDto.Description;
        if (coursePatchDto.AcceptanceMode.HasValue) course.AcceptanceMode = coursePatchDto.AcceptanceMode.Value;
    }
}