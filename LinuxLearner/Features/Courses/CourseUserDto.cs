using LinuxLearner.Features.Users;

namespace LinuxLearner.Features.Courses;

public record CourseUserDto(
    CourseDto Course,
    UserDto User,
    bool IsCourseAdministrator,
    DateTimeOffset JoinTime);
