using AutoFixture;
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
    public async Task GetCoursesAsync_ShouldPerformPaging(Fixture fixture)
    {
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 5, sortParameter: CourseSortParameter.Name),
            courses => courses.OrderBy(c => c.Name).Take(5).ToList());
        
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(2, 5, sortParameter: CourseSortParameter.Name),
            courses => courses.OrderBy(c => c.Name).Skip(5).Take(5).ToList());
        
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(3, 5, sortParameter: CourseSortParameter.Name),
            courses => courses.OrderBy(c => c.Name).Skip(10).Take(5).ToList());
    }

    [Theory, CustomAutoData]
    public async Task GetCoursesAsync_ShouldPerformFiltering(Fixture fixture)
    {
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, name: "1", sortParameter: CourseSortParameter.Name),
            courses => courses
                .OrderBy(c => c.Name)
                .Where(c => c.Name.Contains('1'))
                .ToList());

        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, description: "5", sortParameter: CourseSortParameter.Name),
            courses => courses
                .OrderBy(c => c.Name)
                .Where(c => c.Description is not null && c.Description.Contains('5'))
                .ToList());

        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, acceptanceMode: AcceptanceMode.Closed),
            courses => courses
                .OrderBy(c => c.Name)
                .Where(c => c.AcceptanceMode == AcceptanceMode.Closed)
                .ToList());
    }

    [Theory, CustomAutoData]
    public async Task GetCoursesAsync_ShouldPerformSorting(Fixture fixture)
    {
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, sortParameter: CourseSortParameter.Name),
            courses => courses.OrderBy(c => c.Name).ToList());
        
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, sortParameter: CourseSortParameter.Id),
            courses => courses.OrderBy(c => c.Id).ToList());
        
        await AssertGetCoursesAsync(fixture,
            () => CourseService.GetCoursesAsync(1, 15, sortParameter: CourseSortParameter.Description),
            courses => courses.OrderBy(c => c.Description).ToList());
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

    private async Task AssertGetCoursesAsync(
        Fixture fixture,
        Func<Task<(int, IEnumerable<CourseDto>)>> actualCall,
        Func<List<Course>, List<Course>> expectCall,
        int count = 15)
    {
        var courses = fixture.CreateMany<Course>(count)!.ToList();
        await DbContext.Courses.ExecuteDeleteAsync();
        DbContext.AddRange(courses);
        await DbContext.SaveChangesAsync();

        var (_, returnedCourses) = await actualCall();
        returnedCourses = returnedCourses.ToList();
        var expectedCourses = expectCall(courses);
        returnedCourses.Count().Should().Be(expectedCourses.Count);
        foreach (var course in expectedCourses)
        {
            var courseDto = returnedCourses.FirstOrDefault(c => c.Id == course.Id);
            courseDto.Should().NotBeNull();
            Match(course, courseDto!);
        }
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