using LinuxLearner.Domain;

namespace LinuxLearner.Features.Users;

public record UserDto(
    Guid Id,
    UserType UserType);