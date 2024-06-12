using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public record UserDto(
    Guid Id,
    UserType UserType,
    string Username,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Description);
