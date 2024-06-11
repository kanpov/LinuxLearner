using FluentAssertions;
using Keycloak.AuthServices.Sdk;
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

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Name == userDto.Name);
        user.Should().NotBeNull();
        Match(user!, userDto);
        
        userDto.UserType.Should().Be(userType);
        userDto.Name.Should().Be(httpContext.User.Identity!.Name!);
    }

    [Theory, CustomAutoData]
    public async Task GetUserAsync_ShouldReturnUser_WhenItExists(User user)
    {
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var userDto = await Service.GetUserAsync(user.Name);
        userDto.Should().NotBeNull();
        userDto!.Should()
            .BeEquivalentTo(new UserDto(user.Name, user.UserType, user.Description, user.RegistrationTime));
    }

    [Theory, CustomAutoData]
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
            new UserDto(user.Name, user.UserType, user.Description, user.RegistrationTime));
        var actualDtos = await Service.GetUsersAsync(1, 10);
        expectedDtos.Should().BeEquivalentTo(actualDtos);
    }
    
    [Theory]
    [InlineData(UserType.Student, "student_description")]
    [InlineData(UserType.Teacher, "teacher_description")]
    public async Task PatchAuthorizedUserAsync_ShouldApplyChanges(UserType userType, string description)
    {
        var httpContext = MakeContext(userType);
        var userPatchDto = new UserPatchDto(description);
        await Service.GetAuthorizedUserAsync(httpContext);
        
        var userDto = await Service.PatchAuthorizedUserAsync(httpContext, userPatchDto);
        
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Name == userDto.Name);
        user.Should().NotBeNull();
        Match(user!, userDto);
        Match(userDto, userPatchDto);
    }

    [Theory, CustomAutoData]
    public async Task PatchUserAsync_ShouldApplyChanges_OnExistentUser(User user, UserPatchDto userPatchDto)
    {
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var userDto = await Service.PatchUserAsync(user.Name, userPatchDto);

        userDto.Should().NotBeNull();
        Match(user, userDto!);
        Match(userDto!, userPatchDto);
    }

    [Theory, CustomAutoData]
    public async Task PatchUserAsync_ShouldFail_OnNonExistentUser(string username, UserPatchDto userPatchDto)
    {
        var userDto = await Service.PatchUserAsync(username, userPatchDto);
        userDto.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAuthorizedUserAsync_ShouldSucceed()
    {
        var httpContext = MakeContext(UserType.Student);
        var userDto = await Service.GetAuthorizedUserAsync(httpContext);
        var keycloakUserId = await CreateKeycloakUserAsync(userDto.Name, userDto.UserType);

        await Service.DeleteAuthorizedUserAsync(httpContext);

        await AssertUserIsMissingAsync(userDto.Name, keycloakUserId);
    }

    [Theory, CustomAutoData]
    public async Task DeleteUserAsync_ShouldSucceed_WithExistingUser(User user)
    {
        DbContext.Users.Add(user);
        var keycloakUserId = await CreateKeycloakUserAsync(user);
        await DbContext.SaveChangesAsync();

        var success = await Service.DeleteUserAsync(user.Name);

        success.Should().BeTrue();
        await AssertUserIsMissingAsync(user.Name, keycloakUserId);
    }

    [Theory, CustomAutoData]
    public async Task DeleteUserAsync_ShouldFail_OnNonExistentUser(string username)
    {
        var success = await Service.DeleteUserAsync(username);
        success.Should().BeFalse();
    }

    private async Task AssertUserIsMissingAsync(string username, string keycloakUserId)
    {
        var dbUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Name == username);
        dbUser.Should().BeNull();
        
        await FluentActions
            .Awaiting(async () => await KeycloakUserClient.GetUserAsync("master", keycloakUserId))
            .Should()
            .ThrowAsync<KeycloakHttpClientException>();
    }

    private static void Match(User user, UserDto userDto)
    {
        user.Name.Should().Be(userDto.Name);
        user.Description.Should().Be(userDto.Description);
        user.UserType.Should().Be(userDto.UserType);
        user.RegistrationTime.Should().Be(userDto.RegistrationTime);
    }

    private static void Match(UserDto userDto, UserPatchDto userPatchDto)
    {
        userDto.Description.Should().Be(userPatchDto.Description);
    }
    
    // investigate the possibility of connecting to keycloak so that DELETE endpoints also become testable
}