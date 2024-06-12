using System.Text.Json.Serialization;

namespace LinuxLearner.Domain;

public class User
{
    public Guid Id { get; set; }
    
    public UserType UserType { get; set; } = UserType.Student;

    [JsonIgnore] public List<CourseParticipation> CourseParticipations { get; set; } = [];
    [JsonIgnore] public List<Course> Courses { get; set; } = [];
}