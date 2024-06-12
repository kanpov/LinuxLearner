using FluentAssertions;
using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;
using LinuxLearner.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class CourseServiceTests(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    private CourseService CourseService => Services.GetRequiredService<CourseService>();
    private UserService UserService => Services.GetRequiredService<UserService>();

    [Theory, CustomAutoData]
    public async Task GetCourseAsync_ShouldReturnOne_WhenExists(Course course)
    {
        DbContext.Add(course);
        await DbContext.SaveChangesAsync();

        var courseDto = await CourseService.GetCourseAsync(course.Id);
        courseDto.Should().NotBeNull();
        Match(course, courseDto!);
    }

    [Theory, CustomAutoData]
    public async Task GetCourseAsync_ShouldReturnNone_WhenNonExistent(Guid courseId)
    {
        var courseDto = await CourseService.GetCourseAsync(courseId);
        courseDto.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task CreateCourseAsync_ShouldPersist(CourseCreateDto courseCreateDto)
    {
        var httpContext = MakeContext(UserType.Teacher);
        var courseDto = await CourseService.CreateCourseAsync(httpContext, courseCreateDto);
        
        Match(courseDto, courseCreateDto);
        
        var course = await DbContext.Courses.FirstOrDefaultAsync(c => c.Id == courseDto.Id);
        course.Should().NotBeNull();
        Match(course!, courseDto);

        var courseParticipation = await DbContext.CourseParticipations.FirstOrDefaultAsync(
            p => p.CourseId == course!.Id && p.UserId == GetUserIdFromContext(httpContext));
        courseParticipation.Should().NotBeNull();
        courseParticipation!.IsCourseAdministrator.Should().BeTrue();
    }

    [Theory, CustomAutoData]
    public async Task PatchCourseAsync_ShouldPersist(Course course, CoursePatchDto coursePatchDto)
    {
        var httpContext = await ArrangeParticipation(course);
        var successful = await CourseService.PatchCourseAsync(httpContext, course.Id, coursePatchDto);
        successful.Should().BeTrue();
        Match(course, coursePatchDto);
    }
    
    [Theory, CustomAutoData]
    public async Task ForcePatchCourseAsync_ShouldRejectNonExistentCourse(Guid courseId, CoursePatchDto coursePatchDto)
    {
        var success = await CourseService.ForcePatchCourseAsync(courseId, coursePatchDto);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task ForcePatchCourseAsync_ShouldPersist(Course course, CoursePatchDto coursePatchDto)
    {
        DbContext.Add(course);
        await DbContext.SaveChangesAsync();

        var success = await CourseService.ForcePatchCourseAsync(course.Id, coursePatchDto);
        success.Should().BeTrue();
        Match(course, coursePatchDto);
    }

    [Theory, CustomAutoData]
    public async Task PatchCourseAsync_ShouldRejectNonAdministrator(Course course, CoursePatchDto coursePatchDto)
    {
        var httpContext = await ArrangeParticipation(course, isAdministrator: false);
        var successful = await CourseService.PatchCourseAsync(httpContext, course.Id, coursePatchDto);
        successful.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task DeleteCourseAsync_ShouldPersist(Course course)
    {
        var httpContext = await ArrangeParticipation(course);
        var successful = await CourseService.DeleteCourseAsync(httpContext, course.Id);
        
        successful.Should().BeTrue();
        var queriedCourse = await DbContext.Courses.FirstOrDefaultAsync(c => c.Id == course.Id);
        queriedCourse.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task DeleteCourseAsync_ShouldRejectNonAdministrator(Course course)
    {
        var httpContext = await ArrangeParticipation(course, isAdministrator: false);
        var successful = await CourseService.DeleteCourseAsync(httpContext, course.Id);
        
        successful.Should().BeFalse();
        var queriedCourse = await DbContext.Courses.FirstOrDefaultAsync(c => c.Id == course.Id);
        queriedCourse.Should().NotBeNull();
    }

    [Theory, CustomAutoData]
    public async Task ForceDeleteCourseAsync_ShouldRejectNonExistentCourse(Guid courseId)
    {
        var success = await CourseService.ForceDeleteCourseAsync(courseId);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task ForceDeleteCourseAsync_ShouldPersist(Course course)
    {
        DbContext.Add(course);
        await DbContext.SaveChangesAsync();
        
        var success = await CourseService.ForceDeleteCourseAsync(course.Id);
        success.Should().BeTrue();
        var queriedCourse = await DbContext.Courses.FirstOrDefaultAsync(c => c.Id == course.Id);
        queriedCourse.Should().BeNull();
    }
    
    private async Task<HttpContext> ArrangeParticipation(Course course, bool isAdministrator = true)
    {
        var httpContext = MakeContext(UserType.Teacher);
        DbContext.Add(course);
        await UserService.GetAuthorizedUserAsync(httpContext);
        DbContext.Add(new CourseParticipation
            { CourseId = course.Id, UserId = GetUserIdFromContext(httpContext), IsCourseAdministrator = isAdministrator });
        await DbContext.SaveChangesAsync();

        return httpContext;
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