using AutoFixture;
using AutoFixture.Xunit2;
using LinuxLearner.Domain;

namespace LinuxLearner.IntegrationTests;

public class CustomAutoDataAttribute() : AutoDataAttribute(() =>
{
    var fixture = new Fixture();
    
    // avoid circular dependencies in a many-to-many relationship
    fixture.Customize<User>(u =>
        u.Without(x => x.Courses).Without(x => x.CourseUsers));
    fixture.Customize<Course>(c =>
        c.Without(x => x.Users).Without(x => x.CourseUsers));
    
    return fixture;
});