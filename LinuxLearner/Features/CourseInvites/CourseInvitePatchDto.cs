namespace LinuxLearner.Features.CourseInvites;

public record CourseInvitePatchDto(
    int? UsageLimit = null,
    TimeSpan? LifespanFromNow = null);