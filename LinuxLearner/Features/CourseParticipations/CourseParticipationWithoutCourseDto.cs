using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.CourseParticipations;

public record CourseParticipationWithoutCourseDto(
    UserDto User,
    bool IsCourseAdministrator,
    DateTimeOffset JoinTime);
