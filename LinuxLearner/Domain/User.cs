using LinuxLearner.Features.Users;

namespace LinuxLearner.Domain;

public class User
{
    public string Username { get; set; } = null!;
    
    public UserType UserType { get; set; } = UserType.Student;
    
    public string? Description { get; set; }

    private readonly DateTimeOffset _registrationTime;
    public DateTimeOffset RegistrationTime
    {
        get => _registrationTime;
        init => _registrationTime = value.ToUniversalTime();
    }

    public List<Course> Courses { get; set; } = [];

    public UserDto MapToUserDto() => new(Username, UserType, Description, RegistrationTime);

    public void ProjectUserPatchDto(UserPatchDto userPatchDto)
    {
        Description = userPatchDto.Description;
    }
}