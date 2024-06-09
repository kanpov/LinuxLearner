namespace LinuxLearner.Domain;

public class User
{
    public string Name { get; set; } = null!;
    
    public UserType UserType { get; set; } = UserType.Student;
    
    public string? Description { get; set; }

    public DateTimeOffset RegistrationTime { get; set; } = DateTimeOffset.UtcNow;

    public List<CourseParticipation> CourseUsers { get; set; } = [];
    public List<Course> Courses { get; set; } = [];
}