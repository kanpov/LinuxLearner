using System.Text.Json.Serialization;

namespace LinuxLearner.Domain;

public class CourseParticipation
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    
    [JsonIgnore] public User User { get; set; } = null!;
    [JsonIgnore] public Course Course { get; set; } = null!;
    
    public bool IsCourseAdministrator { get; set; }
    public DateTimeOffset JoinTime { get; set; }
}