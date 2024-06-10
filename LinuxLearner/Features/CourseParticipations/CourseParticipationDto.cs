using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.CourseParticipations;

public record CourseParticipationDto(
    CourseDto Course,
    UserDto User,
    bool IsCourseAdministrator,
    DateTimeOffset JoinTime);
