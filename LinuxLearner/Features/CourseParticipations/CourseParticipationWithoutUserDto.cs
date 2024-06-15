using LinuxLearner.Features.Courses;

namespace LinuxLearner.Features.CourseParticipations;

public record CourseParticipationWithoutUserDto(
    CourseDto Course,
    bool IsCourseAdministrator,
    DateTimeOffset JoinTime);
