using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.CourseParticipations;

public class CourseParticipationService(
    CourseParticipationRepository courseParticipationRepository,
    UserService userService)
{
    public async Task CreateParticipationAsync(CourseParticipation courseParticipation)
    {
        await courseParticipationRepository.AddParticipationAsync(courseParticipation);
    }
    
    public async Task<bool> ChangeAdministrationOnCourseAsync(
        HttpContext httpContext, Guid courseId, string userName, bool isCourseAdministrator)
    {
        var grantor = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (grantor is null) return false;

        var grantee = await courseParticipationRepository.GetParticipationAsync(courseId, userName);
        if (grantee is null
            || grantee is { User.UserType: UserType.Student }
            || grantee.UserName == grantor.UserName) return false;

        grantee.IsCourseAdministrator = isCourseAdministrator;
        await courseParticipationRepository.UpdateParticipationAsync(grantee);
        
        return true;
    }

    public async Task<CourseParticipationDto?> GetParticipationAsync(Guid courseId, string userName)
    {
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, userName);
        return participation is null ? null : MapToCourseParticipationDto(participation);
    }

    public async Task<IEnumerable<CourseParticipationDto>?> GetParticipationsForCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participationOfSelf = await courseParticipationRepository.GetParticipationAsync(courseId, user.Name);
        if (participationOfSelf is null) return null;

        var participations = await courseParticipationRepository.GetParticipationsForCourseAsync(courseId);
        return participations.Select(MapToCourseParticipationDto);
    }

    public async Task<IEnumerable<CourseParticipationDto>> GetParticipationsForUserAsync(string username)
    {
        var participations = await courseParticipationRepository.GetParticipationsForUserAsync(username);
        return participations.Select(MapToCourseParticipationDto);
    }

    public async Task<CourseParticipation?> GetAdministrativeParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var courseUser = await courseParticipationRepository.GetParticipationAsync(courseId, user.Name);

        return courseUser is { IsCourseAdministrator: true } ? courseUser : null;
    }

    public async Task<bool> DeleteOwnParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, user.Name);
        if (participation is null) return false;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
        return true;
    }

    public async Task<bool> DeleteParticipationAsync(HttpContext httpContext, Guid courseId, string username)
    {
        var administrativeParticipation = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (administrativeParticipation is null) return false;

        return await ForceDeleteParticipationAsync(courseId, username);
    }

    public async Task<bool> ForceDeleteParticipationAsync(Guid courseId, string username)
    {
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, username);
        if (participation is null) return false;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
        return true;
    }
    
    private static CourseParticipationDto MapToCourseParticipationDto(CourseParticipation courseParticipation) =>
        new(CourseService.MapToCourseDto(courseParticipation.Course),
            UserService.MapToUserDto(courseParticipation.User),
            courseParticipation.IsCourseAdministrator,
            courseParticipation.JoinTime);
}