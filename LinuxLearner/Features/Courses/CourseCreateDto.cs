using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public record CourseCreateDto(
    string Name,
    string? Description = null,
    AcceptanceMode AcceptanceMode = AcceptanceMode.InviteRequired,
    bool Discoverable = false);
