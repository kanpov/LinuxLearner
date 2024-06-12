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
        HttpContext httpContext, Guid courseId, Guid userId, bool isCourseAdministrator)
    {
        var grantor = await GetAdministrativeParticipationAsync(httpContext, courseId);
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

    public async Task<IEnumerable<CourseParticipationDto>?> GetParticipationsForCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserEntityAsync(httpContext);
        var participationOfSelf = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);
        if (participationOfSelf is null) return null;

        var participations = await courseParticipationRepository.GetParticipationsForCourseAsync(courseId);
        return await FetchParticipationDtosAsync(participations);
    }

    public async Task<IEnumerable<CourseParticipationDto>> GetParticipationsForUserAsync(Guid userId)
    {
        var participations = await courseParticipationRepository.GetParticipationsForUserAsync(userId);
        return await FetchParticipationDtosAsync(participations);
    }

    public async Task<CourseParticipation?> GetAdministrativeParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserEntityAsync(httpContext);
        var courseUser = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);

        return courseUser is { IsCourseAdministrator: true } ? courseUser : null;
    }

    public async Task<bool> DeleteOwnParticipationAsync(HttpContext httpContext, Guid courseId)
    {
        var user = await userService.GetAuthorizedUserEntityAsync(httpContext);
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, user.Id);
        if (participation is null) return false;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
        return true;
    }

    public async Task<bool> DeleteParticipationAsync(HttpContext httpContext, Guid courseId, Guid userId)
    {
        var administrativeParticipation = await GetAdministrativeParticipationAsync(httpContext, courseId);
        if (administrativeParticipation is null) return false;

        return await ForceDeleteParticipationAsync(courseId, userId);
    }

    public async Task<bool> ForceDeleteParticipationAsync(Guid courseId, Guid userId)
    {
        var participation = await courseParticipationRepository.GetParticipationAsync(courseId, userId);
        if (participation is null) return false;

        await courseParticipationRepository.DeleteParticipationAsync(participation);
        return true;
    }

    private async Task<IEnumerable<CourseParticipationDto>> FetchParticipationDtosAsync(
        IEnumerable<CourseParticipation> participations)
    {
        return await participations
            .ToAsyncEnumerable()
            .SelectAwait(async p => await FetchParticipationDtoAsync(p))
            .ToListAsync();
    }
    
    private async Task<CourseParticipationDto> FetchParticipationDtoAsync(CourseParticipation courseParticipation) =>
        new(CourseService.MapToCourseDto(courseParticipation.Course),
            await userService.FetchUserDtoAsync(courseParticipation.User),
            courseParticipation.IsCourseAdministrator,
            courseParticipation.JoinTime);
}