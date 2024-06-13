namespace LinuxLearner.Features.CourseInvites;

public record CourseInviteCreateDto(
    int UsageLimit,
    TimeSpan? Lifespan = null);
