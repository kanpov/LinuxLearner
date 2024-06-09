namespace LinuxLearner.Domain;

public class Course
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public AcceptanceMode AcceptanceMode { get; set; } = AcceptanceMode.NoInviteRequired;

    public List<CourseParticipation> CourseUsers { get; set; } = [];
    public List<User> Users { get; set; } = [];
}