using AutoFixture;
using FluentAssertions;
using LinuxLearner.Domain;
using LinuxLearner.Features.CourseParticipations;
using LinuxLearner.Features.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class CourseParticipationServiceTests(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    private CourseParticipationService CourseParticipationService => Services.GetRequiredService<CourseParticipationService>();
    private UserService UserService => Services.GetRequiredService<UserService>();
    
    [Theory, CustomAutoData]
    public async Task ChangeAdministrationOnCourseAsync_ShouldSucceedWithCorrectData(Course course, Fixture fixture)
    {
        var (httpContext, _, userId) = await ArrangeParticipations(course, fixture);
        var success = await CourseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, course.Id, userId, true);
        
        success.Should().BeTrue();
        var participation = await DbContext.CourseParticipations.FirstOrDefaultAsync(p => 
            p.UserId == userId && p.CourseId == course.Id);
        participation.Should().NotBeNull();
        participation!.IsCourseAdministrator.Should().BeTrue();
    }

    [Theory, CustomAutoData]
    public async Task ChangeAdministrationOnCourseAsync_ShouldRejectNonAdministrator(Course course, Fixture fixture)
    {
        var (httpContext, _, username) = await ArrangeParticipations(course, fixture, ownerIsAdmin: false);
        var success =
            await CourseParticipationService.ChangeAdministrationOnCourseAsync(httpContext, course.Id, username, true);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationAsync_ShouldRejectNonExistentOne(Guid courseId, Guid userId)
    {
        var participationDto = await CourseParticipationService.GetParticipationAsync(courseId, userId);
        participationDto.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationAsync_ShouldReturnOne_WhenItExists(Course course, Fixture fixture)
    {
        var (_, participations, _) = await ArrangeParticipations(course, fixture);
        var participation = participations.First();

        var participationDto =
            await CourseParticipationService.GetParticipationAsync(participation.CourseId, participation.UserId);
        participationDto.Should().NotBeNull();
        Match(participation, participationDto!);
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationsForCourseAsync_ShouldReturnMatches(Course course, Fixture fixture)
    {
        var (httpContext, participations, _) = await ArrangeParticipations(course, fixture);
        var participationDtos =
            await CourseParticipationService.GetParticipationsForCourseAsync(httpContext, course.Id);
        
        participationDtos = participationDtos!.ToList();
        foreach (var participationDto in participationDtos)
        {
            var participation = participations.FirstOrDefault(p =>
                p.UserId == participationDto.User.Id && p.CourseId == participationDto.Course.Id);
            if (participation is null) continue;
            Match(participation, participationDto);
        }
    }

    private async Task<(HttpContext, List<CourseParticipation>, Guid)> ArrangeParticipations(
        Course course, Fixture fixture, int amount = 1, bool ownerIsAdmin = true, UserType ownerType = UserType.Teacher)
    {
        var httpContext = MakeContext(ownerType);
        await UserService.GetAuthorizedUserAsync(httpContext);
        
        DbContext.Courses.Add(course);
        DbContext.Add(new CourseParticipation
            { CourseId = course.Id, UserId = GetUserIdFromContext(httpContext), IsCourseAdministrator = ownerIsAdmin });

        var courseParticipations = new List<CourseParticipation>();
        for (var i = 0; i < amount; ++i)
        {
            var user = fixture.Create<User>();
            user.UserType = UserType.Teacher;
            DbContext.Add(user);

            var participation = new CourseParticipation { CourseId = course.Id, UserId = user.Id, IsCourseAdministrator = false };
            courseParticipations.Add(participation);
            DbContext.Add(participation);
        }

        await DbContext.SaveChangesAsync();

        return (httpContext, courseParticipations, courseParticipations.First().UserId);
    }

    private static void Match(CourseParticipation participation, CourseParticipationDto participationDto)
    {
        participation.CourseId.Should().Be(participationDto.Course.Id);
        participation.UserId.Should().Be(participationDto.User.Id);
        participation.IsCourseAdministrator.Should().Be(participationDto.IsCourseAdministrator);
        participation.JoinTime.Should().Be(participationDto.JoinTime);
    }
}