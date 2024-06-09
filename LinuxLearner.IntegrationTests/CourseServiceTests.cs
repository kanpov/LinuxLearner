using AutoFixture.Xunit2;
using FluentAssertions;
using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class CourseServiceTests(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    private CourseService Service => Services.GetRequiredService<CourseService>();
    private UserService UserService => Services.GetRequiredService<UserService>();

    [Theory, CustomAutoData]
    public async Task GetCourseAsync_ShouldReturnOne_WhenExists(Course course)
    {
        DbContext.Add(course);
        await DbContext.SaveChangesAsync();

        var courseDto = await Service.GetCourseAsync(course.Id);
        courseDto.Should().NotBeNull();
        Match(course, courseDto!);
    }

    [Theory, AutoData]
    public async Task GetCourseAsync_ShouldReturnNone_WhenNonExistent(Guid courseId)
    {
        var courseDto = await Service.GetCourseAsync(courseId);
        courseDto.Should().BeNull();
    }

    [Theory, AutoData]
    public async Task CreateCourseAsync_ShouldPersist(CourseCreateDto courseCreateDto)
    {
        var httpContext = MakeContext(UserType.Teacher);
        var courseDto = await Service.CreateCourseAsync(httpContext, courseCreateDto);
        
        Match(courseDto, courseCreateDto);
        
        var course = await DbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseDto.Id);
        course.Should().NotBeNull();
        Match(course!, courseDto);

        var courseParticipation = await DbContext.CourseParticipations.FirstOrDefaultAsync(
            p => p.CourseId == course!.Id && p.UserName == httpContext.User.Identity!.Name!);
        courseParticipation.Should().NotBeNull();
        courseParticipation!.IsCourseAdministrator.Should().BeTrue();
    }

    [Theory, CustomAutoData]
    public async Task PatchCourseAsync_ShouldPersist(Course course, CoursePatchDto coursePatchDto)
    {
        var httpContext = MakeContext(UserType.Teacher);
        DbContext.Add(course);
        await UserService.GetAuthorizedUserAsync(httpContext);
        DbContext.Add(new CourseParticipation
            { CourseId = course.Id, UserName = httpContext.User.Identity!.Name!, IsCourseAdministrator = true });
        await DbContext.SaveChangesAsync();

        var successful = await Service.PatchCourseAsync(httpContext, course.Id, coursePatchDto);

        successful.Should().BeTrue();
        Match(course, coursePatchDto);
    }

    private static void Match(Course course, CourseDto courseDto)
    {
        course.Id.Should().Be(courseDto.Id);
        course.Name.Should().Be(courseDto.Name);
        course.Description.Should().Be(courseDto.Description);
        course.AcceptanceMode.Should().Be(courseDto.AcceptanceMode);
    }
    
    private static void Match(Course course, CoursePatchDto coursePatchDto)
    {
        course.Name.Should().Be(coursePatchDto.Name);
        course.Description.Should().Be(coursePatchDto.Description);
        course.AcceptanceMode.Should().Be(coursePatchDto.AcceptanceMode);
    }

    private static void Match(CourseDto courseDto, CourseCreateDto courseCreateDto)
    {
        courseDto.Name.Should().Be(courseCreateDto.Name);
        courseDto.Description.Should().Be(courseCreateDto.Description);
        courseDto.AcceptanceMode.Should().Be(courseCreateDto.AcceptanceMode);
    }
}