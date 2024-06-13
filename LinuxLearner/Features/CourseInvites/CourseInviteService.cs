using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteService(CourseInviteRepository inviteRepository, CourseService courseService)
{
    public async Task<CourseInviteDto?> CreateInviteAsync(Guid courseId, CourseInviteCreateDto inviteCreateDto)
    {
        var course = await courseService.GetCourseAsync(courseId);
        if (course is null) return null;
        
        var invite = MapCreateDtoToInvite(courseId, inviteCreateDto);
        await inviteRepository.AddInviteAsync(invite);
        return MapInviteToDto(invite);
    }

    public async Task<CourseInviteDto?> GetInviteAsync(Guid courseId, Guid inviteId)
    {
        var invite = await inviteRepository.GetInviteAsync(inviteId, courseId);
        if (invite is null) return null;

        return MapInviteToDto(invite);
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
}