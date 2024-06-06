using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public record UserDto(
    string Username,
    UserType UserType,
    string? Description,
    DateTimeOffset RegistrationTime);