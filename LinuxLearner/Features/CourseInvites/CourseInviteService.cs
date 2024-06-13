using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Courses;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteService(
    CourseInviteRepository inviteRepository,
    CourseService courseService,
    CourseParticipationService courseParticipationService)
{
    public async Task<CourseInviteDto?> CreateInviteAsync(HttpContext httpContext, Guid courseId,
        CourseInviteCreateDto inviteCreateDto)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return null;
        
        var course = await courseService.GetCourseAsync(courseId);
        if (course is null) return null;
        
        var invite = MapCreateDtoToInvite(courseId, inviteCreateDto);
        await inviteRepository.AddInviteAsync(invite);
        return await GetInviteAsync(invite.Id);
    }

    public async Task<CourseInviteDto?> GetInviteAsync(Guid inviteId)
    {
        var invite = await inviteRepository.GetInviteAsync(inviteId);
        return invite is null ? null : MapInviteToDto(invite);
    }

    public async Task<bool> PatchInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId,
        CourseInvitePatchDto invitePatchDto)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return false;
        
        var invite = await inviteRepository.GetInviteAsync(inviteId);
        if (invite is null) return false;
        
        ProjectPatchDtoToInvite(invite, invitePatchDto);
        await inviteRepository.UpdateInviteAsync(invite);
        return true;
    }

    public async Task<bool> DeleteInviteAsync(HttpContext httpContext, Guid courseId, Guid inviteId)
    {
        var participation = await courseParticipationService.GetAuthorizedParticipationAsync(httpContext, courseId);
        if (participation is null) return false;
        
        var invite = await inviteRepository.GetInviteAsync(inviteId);
        if (invite is null) return false;
        
        await inviteRepository.DeleteInviteAsync(invite);
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