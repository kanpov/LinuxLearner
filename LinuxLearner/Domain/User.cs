using LinuxLearner.Features.Users;

namespace LinuxLearner.Domain;

public class User
{
    public string Username { get; set; } = null!;
    
    public UserType UserType { get; set; } = UserType.Student;
    
    public string? Description { get; set; }
    public DateTimeOffset RegistrationTime { get; set; } = DateTimeOffset.UtcNow;

    public UserDto MapToUserDto() => new(Username, UserType, Description, RegistrationTime);

    public void ProjectUserPatchDto(UserPatchDto userPatchDto)
    {
        Description = userPatchDto.Description;
    }
}