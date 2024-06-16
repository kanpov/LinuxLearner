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
        var (_, participations, _) = await ArrangeParticipations(course, fixture, withRealUsers: true);
        var participation = participations.First();

        var participationDto =
            await CourseParticipationService.GetParticipationAsync(participation.CourseId, participation.UserId);
        participationDto.Should().NotBeNull();
        Matcher.Match(participation, participationDto!);
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationsForCourseAsync_ShouldReturnMatches(Course course1, Course course2,
        Fixture fixture)
    {
        var (httpContext, participations1, _) = await ArrangeParticipations(course1, fixture, withRealUsers: true);
        await ArrangeParticipations(course2, fixture, withRealUsers: true);

        var participationDtos =
            (await CourseParticipationService.GetParticipationsForCourseAsync(httpContext, course1.Id))!.ToList();
        participationDtos.Count.Should().Be(participations1.Count + 1);
        var selfUserId = GetUserIdFromContext(httpContext);

        foreach (var participationDto in participationDtos)
        {
            if (participationDto.User.Id == selfUserId) continue;
            
            var matchingParticipation = participations1.FirstOrDefault(p => p.UserId == participationDto.User.Id);
            matchingParticipation.Should().NotBeNull();
            Matcher.Match(matchingParticipation!, participationDto);
        }
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationsForCourseAsync_ShouldRejectNonParticipant(Course course, Fixture fixture)
    {
        await ArrangeParticipations(course, fixture);
        var httpContext = MakeContext(UserType.Student);

        var participationDtos =
            await CourseParticipationService.GetParticipationsForCourseAsync(httpContext, course.Id);
        participationDtos.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task GetParticipationsForUserAsync_ShouldReturnMatches(Course course, Fixture fixture)
    {
        var (_, participations, _) = await ArrangeParticipations(course, fixture, withRealUsers: true);

        foreach (var participation in participations)
        {
            var participationDtos =
                (await CourseParticipationService.GetParticipationsForUserAsync(participation.UserId)).ToList();

            participationDtos.Count.Should().Be(1);
            var participationDto = participationDtos.First();
            Matcher.Match(participation, participationDto);
        }
    }

    [Theory, CustomAutoData]
    public async Task DeleteParticipationAsync_ShouldRejectNonAdministrator(Course course, Fixture fixture)
    {
        var (_, participations, userIdToDelete) = await ArrangeParticipations(course, fixture);
        var nonAdministratorContext = MakeContext(participations.First().User);
        
        var success = await CourseParticipationService.DeleteParticipationAsync(nonAdministratorContext, course.Id, userIdToDelete);
        success.Should().BeFalse();
        var participation = await DbContext.CourseParticipations.FirstOrDefaultAsync(
            p => p.UserId == userIdToDelete && p.CourseId == course.Id);
        participation.Should().NotBeNull();
    }

    [Theory, CustomAutoData]
    public async Task DeleteParticipationAsync_ShouldRejectNonExistentParticipation(Course course, Fixture fixture)
    {
        var (httpContext, _, _) = await ArrangeParticipations(course, fixture);
        var userIdToDelete = Guid.NewGuid();

        var success = await CourseParticipationService.DeleteParticipationAsync(httpContext, course.Id, userIdToDelete);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task DeleteParticipationAsync_ShouldPersist(Course course, Fixture fixture)
    {
        var (httpContext, _, userIdToDelete) = await ArrangeParticipations(course, fixture);
        var success = await CourseParticipationService.DeleteParticipationAsync(httpContext, course.Id, userIdToDelete);

        success.Should().BeTrue();
        var participation = await DbContext.CourseParticipations.FirstOrDefaultAsync(
            p => p.CourseId == course.Id && p.UserId == userIdToDelete);
        participation.Should().BeNull();
    }

    private async Task<(HttpContext, List<CourseParticipation>, Guid)> ArrangeParticipations(
        Course course, Fixture fixture, int amount = 5, bool ownerIsAdmin = true, UserType ownerType = UserType.Teacher,
        bool withRealUsers = false)
    {
        DefaultHttpContext httpContext;
        if (withRealUsers)
        {
            httpContext = await MakeContextAndUserAsync(ownerType);
        }
        else
        {
            httpContext = MakeContext(ownerType);
        }
        
        await UserService.GetAuthorizedUserAsync(httpContext);
        
        DbContext.Courses.Add(course);
        DbContext.Add(new CourseParticipation
            { CourseId = course.Id, UserId = GetUserIdFromContext(httpContext), IsCourseAdministrator = ownerIsAdmin });

        var courseParticipations = new List<CourseParticipation>();
        for (var i = 0; i < amount; ++i)
        {
            var user = fixture.Create<User>();
            user.UserType = UserType.Teacher;
            if (withRealUsers)
            {
                user.Id = await CreateKeycloakUserAsync(user.UserType);
            }
            DbContext.Add(user);

            var participation = new CourseParticipation { CourseId = course.Id, UserId = user.Id, IsCourseAdministrator = false };
            courseParticipations.Add(participation);
            DbContext.Add(participation);
        }

        await DbContext.SaveChangesAsync();

        return (httpContext, courseParticipations, courseParticipations.First().UserId);
    }
}