using LinuxLearner.Domain;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.Courses;

public class CourseService(CourseRepository courseRepository, UserService userService)
{
    public async Task<CourseDto> CreateCourseAsync(HttpContext httpContext, CourseCreateDto courseCreateDto)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var course = MapToCourse(courseCreateDto);
        await courseRepository.AddCourseAsync(course);

        await courseRepository.AddCourseUserAsync(new CourseUser
        {
            CourseId = course.Id,
            UserName = user.Name,
            IsCourseAdministrator = true,
            JoinTime = DateTimeOffset.UtcNow
        });
        
        return MapToCourseDto(course);
    }

    public async Task<CourseDto?> GetCourseAsync(Guid id)
    {
        var course = await courseRepository.GetCourseAsync(id);
        return course is null ? null : MapToCourseDto(course);
    }

    public async Task<bool> PatchCourseAsync(HttpContext httpContext, Guid courseId, CoursePatchDto coursePatchDto)
    {
        var courseUser = await GetAdministrativeCourseUserAsync(httpContext, courseId);
        if (courseUser is null) return false;
        
        ProjectCoursePatchDto(courseUser.Course, coursePatchDto);
        await courseRepository.UpdateCourseAsync(courseUser.Course);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var courseUser = await GetAdministrativeCourseUserAsync(httpContext, courseId);
        if (courseUser is null) return false;

        await courseRepository.DeleteCourseAsync(courseId);
        return true;
    }

    public async Task<bool> ChangeAdministrationOnCourseAsync(
        HttpContext httpContext, Guid courseId, string userName, bool isCourseAdministrator)
    {
        var grantorCourseUser = await GetAdministrativeCourseUserAsync(httpContext, courseId);
        if (grantorCourseUser is null) return false;

        var granteeCourseUser = await courseRepository.GetCourseUserAsync(courseId, userName);
        if (granteeCourseUser is not { User.UserType: UserType.Teacher }
            || granteeCourseUser.UserName == grantorCourseUser.UserName) return false;

        granteeCourseUser.IsCourseAdministrator = isCourseAdministrator;
        await courseRepository.UpdateCourseUserAsync(granteeCourseUser);
        
        return true;
    }

    private async Task<CourseUser?> GetAdministrativeCourseUserAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var courseUser = await courseRepository.GetCourseUserAsync(courseId, user.Name);

        return courseUser is { IsCourseAdministrator: true } ? courseUser : null;
    }
    
    private static CourseDto MapToCourseDto(Course course) =>
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