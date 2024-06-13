using LinuxLearner.Features.Courses;

namespace LinuxLearner.Features.CourseInvites;

public record CourseInviteDto(
    Guid Id,
    CourseDto Course,
    DateTimeOffset? ExpirationTime,
    int UsageLimit,
    int UsageAmount);
