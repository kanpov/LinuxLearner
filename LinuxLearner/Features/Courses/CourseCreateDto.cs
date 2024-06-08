using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public record CourseCreateDto(
    string Name,
    string? Description,
    AcceptanceMode AcceptanceMode);
