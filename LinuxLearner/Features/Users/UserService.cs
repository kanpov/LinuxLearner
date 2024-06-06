using System.Security.Claims;
using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public class UserService(UserRepository userRepository)
{
    public async Task<UserDto> GetStudentAsync(ClaimsPrincipal claimsPrincipal)
    {
        return await GetUserAsync(claimsPrincipal, UserType.Student);
    }

    public async Task<UserDto> GetTeacherAsync(ClaimsPrincipal claimsPrincipal)
    {
        return await GetUserAsync(claimsPrincipal, UserType.Teacher);
    }
    
    private async Task<UserDto> GetUserAsync(ClaimsPrincipal claimsPrincipal, UserType userType)
    {
        var username = claimsPrincipal.Identity!.Name!;
        var user = await userRepository.GetUserAsync(username);

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
}