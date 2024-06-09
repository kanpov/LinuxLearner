using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.Courses;

public record CourseParticipationDto(
    CourseDto Course,
    UserDto User,
    bool IsCourseAdministrator,
    DateTimeOffset JoinTime);
