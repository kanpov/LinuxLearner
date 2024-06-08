using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public record CourseDto(
    Guid Id,
    string Name,
    string? Description,
    AcceptanceMode AcceptanceMode);
