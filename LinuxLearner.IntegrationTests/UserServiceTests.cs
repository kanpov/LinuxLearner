using FluentAssertions;
using Keycloak.AuthServices.Sdk;
using LinuxLearner.Domain;
using LinuxLearner.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinuxLearner.IntegrationTests;

public class UserServiceTests(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    private UserService UserService => Services.GetRequiredService<UserService>();
    
    [Theory]
    [InlineData(UserType.Student), InlineData(UserType.Teacher)]
    public async Task GetAuthorizedUserAsync_ShouldCreateUser(UserType userType)
    {
        var httpContext = await MakeContextAndUserAsync(userType);
        var userDto = await UserService.GetAuthorizedUserAsync(httpContext);

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userDto.Id);
        user.Should().NotBeNull();
        Match(user!, userDto);
        
        userDto.UserType.Should().Be(userType);
        userDto.Id.Should().Be(GetUserIdFromContext(httpContext));
    }

    [Theory, CustomAutoData]
    public async Task GetUserAsync_ShouldReturnUser_WhenItExists(User user)
    {
        user.Id = await CreateKeycloakUserAsync(user);
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var userDto = await UserService.GetUserAsync(user.Id);
        userDto.Should().NotBeNull();
    }

    [Theory, CustomAutoData]
    public async Task GetUserAsync_ShouldReturnNull_WithNoUserExisting(Guid userId)
    {
        var userDto = await UserService.GetUserAsync(userId);
        userDto.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAuthorizedUserAsync_ShouldSucceed()
    {
        var httpContext = await MakeContextAndUserAsync(UserType.Student);
        var userDto = await UserService.GetAuthorizedUserAsync(httpContext);

        await UserService.DeleteAuthorizedUserAsync(httpContext);

        await AssertUserIsMissingAsync(userDto.Id);
    }

    [Theory, CustomAutoData]
    public async Task DeleteUserAsync_ShouldSucceed_WithExistingUser(User user)
    {
        DbContext.Users.Add(user);
        var keycloakUserId = await CreateKeycloakUserAsync(user);
        user.Id = keycloakUserId;
        await DbContext.SaveChangesAsync();

        var success = await UserService.DeleteUserAsync(user.Id);

        success.Should().BeTrue();
        await AssertUserIsMissingAsync(user.Id);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_ShouldRejectSamePerson()
    {
        var httpContext = await MakeContextAndUserAsync(UserType.Student);
        var user = await UserService.GetAuthorizedUserAsync(httpContext);

        var success = await UserService.ChangeUserRoleAsync(httpContext, user.Id, demote: false);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task ChangeUserRoleAsync_ShouldRejectInsufficientPrivilege(User granteeUser)
    {
        var httpContext = await MakeContextAndUserAsync(UserType.Student);
        await UserService.GetAuthorizedUserAsync(httpContext);
        granteeUser.UserType = UserType.Teacher;

        var success = await UserService.ChangeUserRoleAsync(httpContext, granteeUser.Id, demote: false);
        success.Should().BeFalse();
    }

    [Theory, CustomAutoData]
    public async Task ChangeUserRoleAsync_ShouldPromote(User granteeUser1, User granteeUser2)
    {
        await AssertPromoteOrDemoteAsync(granteeUser1, UserType.Admin, UserType.Teacher, UserType.Admin, false,
            "admins");
        await AssertPromoteOrDemoteAsync(granteeUser2, UserType.Teacher, UserType.Student, UserType.Teacher, false,
            "teachers");
    }

    [Theory, CustomAutoData]
    public async Task ChangeUserRoleAsync_ShouldDemote(User granteeUser)
    {
        await AssertPromoteOrDemoteAsync(granteeUser, UserType.Admin, UserType.Teacher, UserType.Student, true,
            "students");
    }

    [Theory, CustomAutoData]
    public async Task DeleteUserAsync_ShouldFail_OnNonExistentUser(Guid userId)
    {
        var success = await UserService.DeleteUserAsync(userId);
        success.Should().BeFalse();
    }

    private async Task AssertPromoteOrDemoteAsync(User granteeUser, UserType grantorType, UserType granteeType,
        UserType granteeNextType, bool demote, string groupName)
    {
        var httpContext = await MakeContextAndUserAsync(grantorType);
        var grantorUser = await UserService.GetAuthorizedUserAsync(httpContext);

        granteeUser.Id = await CreateKeycloakUserAsync(granteeType);
        granteeUser.UserType = granteeType;
        DbContext.Add(granteeUser);
        await DbContext.SaveChangesAsync();

        var success = await UserService.ChangeUserRoleAsync(httpContext, granteeUser.Id, demote);

        success.Should().BeTrue();
        grantorUser.UserType.Should().Be(grantorType);
        granteeUser.UserType.Should().Be(granteeNextType);
        var keycloakGroups = await KeycloakUserClient.GetUserGroupsAsync(
            "master", granteeUser.Id.ToString());
        keycloakGroups.Any(g => g.Name == groupName).Should().BeTrue();
    }

    private async Task AssertUserIsMissingAsync(Guid userId)
    {
        var dbUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        dbUser.Should().BeNull();
        
        await FluentActions
            .Awaiting(async () => await KeycloakUserClient.GetUserAsync("master", userId.ToString()))
            .Should()
            .ThrowAsync<KeycloakHttpClientException>();
    }

    private static void Match(User user, UserDto userDto)
    {
        user.Id.Should().Be(userDto.Id);
        user.UserType.Should().Be(userDto.UserType);
    }

    private static void Match(UserDto userDto, UserPatchDto userPatchDto)
    {
        userDto.Description.Should().Be(userPatchDto.Description);
    }
    
    // investigate the possibility of connecting to keycloak so that DELETE endpoints also become testable
}