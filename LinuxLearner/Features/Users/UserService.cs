using System.Security.Claims;
using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public class UserService(UserRepository userRepository)
{
    public async Task<UserDto> GetAuthorizedUserAsync(ClaimsPrincipal claimsPrincipal)
    {
        var username = claimsPrincipal.Identity!.Name!;
        var user = await userRepository.GetUserAsync(username);
        var isStudent = claimsPrincipal.Claims.First(c => c.Type == "resource_access")
            .Value.Contains("student");
        var userType = isStudent ? UserType.Student : UserType.Teacher;

        if (user is not null) return user.MapToUserDto();

        var newUser = new User
        {
            Username = username,
            UserType = userType,
            Description = null,
            RegistrationTime = DateTimeOffset.UtcNow
        };
        await userRepository.AddUserAsync(newUser);

        return newUser.MapToUserDto();
    }

    public async Task<UserDto?> GetUserAsync(string username)
    {
        var user = await userRepository.GetUserAsync(username);
        return user?.MapToUserDto();
    }
}