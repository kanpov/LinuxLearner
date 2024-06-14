using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteService(
    CourseInviteRepository inviteRepository,
    CourseService courseService,
    CourseParticipationService courseParticipationService,
    UserService userService)
{
    public async Task<CourseInviteDto?> CreateInviteAsync(HttpContext httpContext, Guid courseId,
        CourseInviteCreateDto inviteCreateDto)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return null;
        
        var course = await courseService.GetCourseEntityAsync(courseId);
        if (course is null) return null;
        
        var invite = MapCreateDtoToInvite(courseId, inviteCreateDto);
        await inviteRepository.AddInviteAsync(invite);
        return MapInviteToDto(invite);
    }

    public async Task<CourseInviteDto?> GetInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId, adminOnly: false);
        if (participation is null) return null;
        
        var invite = await inviteRepository.GetInviteAsync(courseId, inviteId);
        return invite is null ? null : MapInviteToDto(invite);
    }

    public async Task<bool> PatchInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId,
        CourseInvitePatchDto invitePatchDto)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return false;
        
        var invite = await inviteRepository.GetInviteAsync(courseId, inviteId);
        if (invite is null) return false;
        
        ProjectPatchDtoToInvite(invite, invitePatchDto);
        await inviteRepository.UpdateInviteAsync(invite);
        return true;
    }

    public async Task<bool> DeleteInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return false;
        
        var invite = await inviteRepository.GetInviteAsync(courseId, inviteId);
        if (invite is null) return false;
        
        await inviteRepository.DeleteInviteAsync(invite);
        return true;
    }

    public async Task<IEnumerable<CourseInviteDto>?> GetInvitesForCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId, adminOnly: false);
        if (participation is null) return null;

        var inviteDtos = await inviteRepository.GetInvitesForCourseAsync(courseId);
        return inviteDtos.Select(MapInviteToDto);
    }

    public async Task<bool> JoinCourseWithoutInviteAsync(HttpContext httpContext, Guid courseId)
    {
        var existingParticipation =
            await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId, adminOnly: false);
        if (existingParticipation is not null) return false;
        
        var course = await courseService.GetCourseEntityAsync(courseId);
        if (course is not { AcceptanceMode: AcceptanceMode.NoInviteRequired }) return false;
        
        var user = await userService.GetAuthorizedUserAsync(httpContext);
        await courseParticipationService.CreateParticipationAsync(course, user, isAdministrator: false);
        return true;
    }

    public async Task<bool> JoinCourseWithInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId)
    {
        var existingParticipation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId, adminOnly: false);
        if (existingParticipation is not null) return false;

        var course = await courseService.GetCourseEntityAsync(courseId);
        if (course is null || course.AcceptanceMode == AcceptanceMode.Closed) return false;

        var invite = await inviteRepository.GetInviteAsync(courseId, inviteId);
        if (invite is null
            || (invite.ExpirationTime is not null && invite.ExpirationTime < DateTimeOffset.UtcNow)
            || invite.UsageAmount >= invite.UsageLimit) return false;

        var user = await userService.GetAuthorizedUserAsync(httpContext);
        await courseParticipationService.CreateParticipationAsync(course, user, isAdministrator: false);
        await inviteRepository.IncrementInviteUsageAmountAsync(invite);

        return true;
    }

    public async Task<bool> LeaveCourseAsync(HttpContext httpContext, Guid courseId)
    {
        var participation =
            await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId, adminOnly: false);
        if (participation is null) return false;

        await courseParticipationService.ForceDeleteParticipationAsync(courseId, participation.UserId);
        return true;
    }

    private static CourseInvite MapCreateDtoToInvite(Guid courseId, CourseInviteCreateDto inviteCreateDto)
    {
        var invite = new CourseInvite
        {
            CourseId = courseId,
            UsageAmount = 0,
            UsageLimit = inviteCreateDto.UsageLimit
        };
        if (inviteCreateDto.Lifespan is not null)
        {
            invite.ExpirationTime = DateTimeOffset.UtcNow + inviteCreateDto.Lifespan;
        }

        return invite;
    }

    private static CourseInviteDto MapInviteToDto(CourseInvite invite) =>
        new(invite.Id,
            CourseService.MapToCourseDto(invite.Course),
            invite.ExpirationTime,
            invite.UsageLimit,
            invite.UsageAmount);

    private static void ProjectPatchDtoToInvite(CourseInvite invite, CourseInvitePatchDto invitePatchDto)
    {
        if (invitePatchDto.UsageLimit.HasValue)
        {
            invite.UsageLimit = invitePatchDto.UsageLimit.Value;
        }
        
        if (invitePatchDto.LifespanFromNow.HasValue)
        {
            invite.ExpirationTime = DateTimeOffset.UtcNow + invitePatchDto.LifespanFromNow.Value;
        }
    }
}