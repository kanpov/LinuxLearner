using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public class UserService(UserRepository userRepository)
{
    private const int MaxPageSize = 10;
    
    public async Task<UserDto> GetAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        return user.MapToUserDto();
    }
    
    public async Task<UserDto?> PatchAuthorizedUserAsync(HttpContext httpContext, UserPatchDto userPatchDto)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        
        user.ProjectUserPatchDto(userPatchDto);
        await userRepository.UpdateUserAsync(user);

        return user.MapToUserDto();
    }

    public async Task DeleteAuthorizedUserAsync(HttpContext httpContext)
    {
        var user = await GetAuthorizedUserEntityAsync(httpContext);
        await userRepository.DeleteUserAsync(user.Username);
    }

    public async Task<UserDto?> GetUserAsync(string username)
    {
        var user = await userRepository.GetUserAsync(username);
        return user?.MapToUserDto();
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(int page, int pageSize)
    {
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var users = await userRepository.GetUsersAsync(page, pageSize);
        return users.Select(u => u.MapToUserDto());
    }

    private async Task<User> GetAuthorizedUserEntityAsync(HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;
        
        var username = claimsPrincipal.Identity!.Name!;
        var user = await userRepository.GetUserAsync(username);
        var isStudent = claimsPrincipal.Claims.First(c => c.Type == "resource_access")
            .Value.Contains("student");
        var userType = isStudent ? UserType.Student : UserType.Teacher;

        if (user is not null) return user;

        var newUser = new User
        {
            Username = username,
            UserType = userType,
            Description = null,
            RegistrationTime = DateTimeOffset.UtcNow
        };
        await userRepository.AddUserAsync(newUser);

        return newUser;
    }
}