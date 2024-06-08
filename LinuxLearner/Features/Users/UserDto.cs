using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public record UserDto(
    string Name,
    UserType UserType,
    string? Description,
    DateTimeOffset RegistrationTime);