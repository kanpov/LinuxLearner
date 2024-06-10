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
    private CourseParticipationService Service => Services.GetRequiredService<CourseParticipationService>();
    private UserService UserService => Services.GetRequiredService<UserService>();
    
    [Theory, CustomAutoData]
    public async Task ChangeAdministrationOnCourseAsync_ShouldSucceedWithCorrectData(Course course, Fixture fixture)
    {
        var (httpContext, participations) = await ArrangeParticipations(course, fixture);
        var userName = participations.First().UserName;
        
        var successful = await Service.ChangeAdministrationOnCourseAsync(httpContext, course.Id, userName, true);
        
        successful.Should().BeTrue();
        var participation = await DbContext.CourseParticipations.FirstOrDefaultAsync(p =>
            p.UserName == userName && p.CourseId == course.Id);
        participation.Should().NotBeNull();
        participation!.IsCourseAdministrator.Should().BeTrue();
    }

    private async Task<(HttpContext, List<CourseParticipation>)> ArrangeParticipations(Course course, Fixture fixture, int amount = 1)
    {
        var httpContext = MakeContext(UserType.Teacher);
        await UserService.GetAuthorizedUserAsync(httpContext);
        
        DbContext.Courses.Add(course);
        DbContext.Add(new CourseParticipation
            { CourseId = course.Id, UserName = httpContext.User.Identity!.Name!, IsCourseAdministrator = true });

        var courseParticipations = new List<CourseParticipation>();
        for (var i = 0; i < amount; ++i)
        {
            var user = fixture.Create<User>();
            user.UserType = UserType.Teacher;
            DbContext.Add(user);

            var participation = new CourseParticipation { CourseId = course.Id, UserName = user.Name, IsCourseAdministrator = false };
            courseParticipations.Add(participation);
            DbContext.Add(participation);
        }

        await DbContext.SaveChangesAsync();

        return (httpContext, courseParticipations);
    }
}