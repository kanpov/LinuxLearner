using FluentAssertions;
using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;

namespace LinuxLearner.IntegrationTests;

public static class Matcher
{
    public static void Match(User user, UserDto userDto)
    {
        user.Id.Should().Be(userDto.Id);
        user.UserType.Should().Be(userDto.UserType);
    }
    
    public static void Match(Course course, CourseDto courseDto)
    {
        course.Id.Should().Be(courseDto.Id);
        course.Name.Should().Be(courseDto.Name);
        course.Description.Should().Be(courseDto.Description);
        course.AcceptanceMode.Should().Be(courseDto.AcceptanceMode);
    }
    
    public static void Match(Course course, CoursePatchDto coursePatchDto)
    {
        course.Name.Should().Be(coursePatchDto.Name);
        course.Description.Should().Be(coursePatchDto.Description);
        course.AcceptanceMode.Should().Be(coursePatchDto.AcceptanceMode);
    }

    public static void Match(CourseDto courseDto, CourseCreateDto courseCreateDto)
    {
        courseDto.Name.Should().Be(courseCreateDto.Name);
        courseDto.Description.Should().Be(courseCreateDto.Description);
        courseDto.AcceptanceMode.Should().Be(courseCreateDto.AcceptanceMode);
    }
    
    public static void Match(CourseParticipation participation, CourseParticipationDto participationDto)
    {
        Match(participation.Course, participationDto.Course);
        Match(participation.User, participationDto.User);
        participation.IsCourseAdministrator.Should().Be(participationDto.IsCourseAdministrator);
        participation.JoinTime.Should().Be(participationDto.JoinTime);
    }

    public static void Match(CourseParticipation participation, CourseParticipationWithoutCourseDto participationDto)
    {
        Match(participation.User, participationDto.User);
        participation.IsCourseAdministrator.Should().Be(participationDto.IsCourseAdministrator);
        participation.JoinTime.Should().Be(participationDto.JoinTime);
    }

    public static void Match(CourseParticipation participation, CourseParticipationWithoutUserDto participationDto)
    {
        Match(participation.Course, participationDto.Course);
        participation.IsCourseAdministrator.Should().Be(participationDto.IsCourseAdministrator);
        participation.JoinTime.Should().Be(participationDto.JoinTime);
    }
}