using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.CourseParticipations;

public class CourseParticipationService(
    CourseParticipationRepository courseParticipationRepository,
    UserService userService)
{
    public async Task CreateParticipationAsync(Course course, User user, bool isAdministrator)
    {
        await courseParticipationRepository.AddParticipationAsync(new CourseParticipation
        {
            CourseId = course.Id,
            UserId = user.Id,
            IsCourseAdministrator = isAdministrator,
            JoinTime = DateTimeOffset.UtcNow
        });
    }
    
    public async Task<bool> ChangeAdministrationOnCourseAsync(
        HttpContext httpContext, Guid courseId, Guid userId, bool isCourseAdministrator)
    {
        var grantor = await GetAuthorizedParticipationAsync(httpContext, courseId);
        if (grantor is null) return false;

        var grantee = await courseParticipationRepository.GetParticipationAsync(courseId, userId);
        if (grantee is null
            || grantee is { User.UserType: UserType.Student }
            || grantee.UserId == grantor.UserId) return false;

        grantee.IsCourseAdministrator = isCourseAdministrator;
        await courseParticipationRepository.UpdateParticipationAsync(grantee);
        
        return true;
    }

    public async Task<CourseParticipationDto?> GetParticipationAsync(Guid courseId, Guid userId)
    {
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, userId);
        return participation is null ? null : await FetchParticipationDtoAsync(participation);
    }

    public async Task<IEnumerable<CourseParticipationWithoutCourseDto>?> GetParticipationsForCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participationOfSelf = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);
        if (participationOfSelf is null) return null;

        var participations = await courseParticipationRepository
            .GetParticipationsForCourseAsync(courseId, fetchCourses: false, fetchUsers: true);
        return await FetchParticipationDtosWithoutCourseAsync(participations);
    }

    public async Task<IEnumerable<CourseParticipationWithoutUserDto>> GetParticipationsForUserAsync(Guid userId)
    {
        var participations = await courseParticipationRepository.GetParticipationsForUserAsync(userId);
        return FetchParticipationDtosWithoutUser(participations);
    }

    internal async Task<CourseParticipation?> GetAuthorizedParticipationAsync(HttpContext httpContext, Guid courseId, bool adminOnly = true)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);
        if (participation is null) return null;

        if (adminOnly) return participation is { IsCourseAdministrator: true } ? participation : null;
        return participation;
    }

    public async Task DeleteOwnParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);
        if (participation is null) return;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
    }

    public async Task<bool> DeleteParticipationAsync(HttpContext httpContext, Guid courseId, Guid userId)
    {
        var administrativeParticipation = await GetAuthorizedParticipationAsync(httpContext, courseId);
        if (administrativeParticipation is null) return false;

        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, userId);
        if (participation is null) return false;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
        return true;
    }

    private async Task<IEnumerable<CourseParticipationWithoutCourseDto>> FetchParticipationDtosWithoutCourseAsync(
        IEnumerable<CourseParticipation> participations)
    {
        return await participations
            .ToAsyncEnumerable()
            .SelectAwait(async p => await FetchParticipationDtoWithoutCourseAsync(p))
            .ToListAsync();
    }

    private static IEnumerable<CourseParticipationWithoutUserDto> FetchParticipationDtosWithoutUser(
        IEnumerable<CourseParticipation> participations)
    {
        return participations.Select(p => new CourseParticipationWithoutUserDto(
            CourseService.MapToCourseDto(p.Course),
            p.IsCourseAdministrator,
            p.JoinTime));
    }

    private async Task<CourseParticipationDto> FetchParticipationDtoAsync(CourseParticipation participation) =>
        new(CourseService.MapToCourseDto(participation.Course),
            await userService.FetchUserDtoAsync(participation.User),
            participation.IsCourseAdministrator,
            participation.JoinTime);
    
    private async Task<CourseParticipationWithoutCourseDto> FetchParticipationDtoWithoutCourseAsync(CourseParticipation participation) =>
        new(await userService.FetchUserDtoAsync(participation.User),
            participation.IsCourseAdministrator,
            participation.JoinTime);
}