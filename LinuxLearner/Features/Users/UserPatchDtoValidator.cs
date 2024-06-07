using FluentValidation;

namespace LinuxLearner.Features.Users;

public class UserPatchDtoValidator : AbstractValidator<UserPatchDto>
{
    public UserPatchDtoValidator()
    {
        When(u => u.Description != null, () =>
        {
            RuleFor(u => u.Description)
                .MinimumLength(10)
                .MaximumLength(100);
        });
    }
}