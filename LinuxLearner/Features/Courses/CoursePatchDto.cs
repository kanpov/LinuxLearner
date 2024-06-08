using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public record CoursePatchDto(
    string? Name = null,
    string? Description = null,
    AcceptanceMode? AcceptanceMode = null);