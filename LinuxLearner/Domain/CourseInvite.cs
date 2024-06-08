namespace LinuxLearner.Domain;

public class CourseInvite
{
    public Guid Id { get; set; }
    
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTimeOffset? ExpirationTime { get; set; }
    public int UsageLimit { get; set; }
}