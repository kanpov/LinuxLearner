using AutoFixture.Xunit2;
using FluentAssertions;
using LinuxLearner.Domain;
using LinuxLearner.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class UserServiceTests(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    private UserService Service => Services.GetRequiredService<UserService>();
    
    [Theory]
    [InlineData(UserType.Student), InlineData(UserType.Teacher)]
    public async Task GetAuthorizedUserAsync_ShouldCreateUser(UserType userType)
    {
        var httpContext = MakeContext(userType);
        var userDto = await Service.GetAuthorizedUserAsync(httpContext);

        await AssertUserExistenceAsync(userDto);
        userDto.UserType.Should().Be(userType);
        userDto.Username.Should().Be(httpContext.User.Identity!.Name!);
    }

    [Theory]
    [InlineData(UserType.Student, "student_description")]
    [InlineData(UserType.Teacher, "teacher_description")]
    public async Task PatchAuthorizedUserAsync_ShouldModifyDescription(UserType userType, string description)
    {
        var httpContext = MakeContext(userType);
        await Service.GetAuthorizedUserAsync(httpContext);
        var userDto = await Service.PatchAuthorizedUserAsync(httpContext, new UserPatchDto(description));

        await AssertUserExistenceAsync(userDto);
        userDto.Description.Should().Be(description);
    }

    [Theory]
    [InlineData(UserType.Student), InlineData(UserType.Teacher)]
    public async Task DeleteAuthorizedUserAsync_ShouldRemove(UserType userType)
    {
        var httpContext = MakeContext(userType);
        var initialDto = await Service.GetAuthorizedUserAsync(httpContext);
        await Service.DeleteAuthorizedUserAsync(httpContext);

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Username == initialDto.Username);
        user.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task GetUserAsync_ShouldReturnUser_WhenItExists(User user)
    {
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var userDto = await Service.GetUserAsync(user.Username);
        userDto.Should().NotBeNull();
        userDto!.Should()
            .BeEquivalentTo(new UserDto(user.Username, user.UserType, user.Description, user.RegistrationTime));
    }

    [Theory, AutoData]
    public async Task GetUserAsync_ShouldReturnNull_WithNoUserExisting(string username)
    {
        var userDto = await Service.GetUserAsync(username);
        userDto.Should().BeNull();
    }

    [Theory, CustomAutoData]
    public async Task GetUsersAsync_ShouldReturnAllMatching(List<User> users)
    {
        await DbContext.Users.ExecuteDeleteAsync();
        DbContext.AddRange(users);
        await DbContext.SaveChangesAsync();

        var expectedDtos = users.Select(user =>
            new UserDto(user.Username, user.UserType, user.Description, user.RegistrationTime));
        var actualDtos = await Service.GetUsersAsync(1, 10);
        expectedDtos.Should().BeEquivalentTo(actualDtos);
    }

    private async Task AssertUserExistenceAsync(UserDto userDto)
    {
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Username == userDto.Username);
        user.Should().NotBeNull();
        user.Should().BeEquivalentTo(userDto);
    }
}