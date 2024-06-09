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

        await courseRepository.AddParticipationAsync(new CourseParticipation
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
        var courseUser = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (courseUser is null) return false;
        
        ProjectCoursePatchDto(courseUser.Course, coursePatchDto);
        await courseRepository.UpdateCourseAsync(courseUser.Course);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var courseUser = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (courseUser is null) return false;

        await courseRepository.DeleteCourseAsync(courseId);
        return true;
    }

    public async Task<bool> ChangeAdministrationOnCourseAsync(
        HttpContext httpContext, Guid courseId, string userName, bool isCourseAdministrator)
    {
        var grantor = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (grantor is null) return false;

        var grantee = await courseRepository.GetParticipationAsync(courseId, userName);
        if (grantee is not { User.UserType: UserType.Teacher }
            || grantee.UserName == grantor.UserName) return false;

        grantee.IsCourseAdministrator = isCourseAdministrator;
        await courseRepository.UpdateParticipationAsync(grantee);
        
        return true;
    }

    public async Task<CourseParticipationDto?> GetParticipationAsync(Guid courseId, string userName)
    {
        var participation = await courseRepository.GetParticipationAsync(courseId, userName);
        return participation is null ? null : MapToCourseParticipationDto(participation);
    }

    public async Task<IEnumerable<CourseParticipationDto>?> GetParticipationsForCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participationOfSelf = await courseRepository.GetParticipationAsync(courseId, user.Name);
        if (participationOfSelf is null) return null;

        var participations = await courseRepository.GetParticipationsForCourseAsync(courseId);
        return participations.Select(MapToCourseParticipationDto);
    }

    public async Task<IEnumerable<CourseParticipationDto>> GetParticipationsForUserAsync(string username)
    {
        var participations = await courseRepository.GetParticipationsForUserAsync(username);
        return participations.Select(MapToCourseParticipationDto);
    }

    private async Task<CourseParticipation?> GetAdministrativeParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var courseUser = await courseRepository.GetParticipationAsync(courseId, user.Name);

        return courseUser is { IsCourseAdministrator: true } ? courseUser : null;
    }

    private static CourseParticipationDto MapToCourseParticipationDto(CourseParticipation courseParticipation) =>
        new(MapToCourseDto(courseParticipation.Course), UserService.MapToUserDto(courseParticipation.User),
            courseParticipation.IsCourseAdministrator, courseParticipation.JoinTime);
    
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