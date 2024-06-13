using FluentValidation;

namespace LinuxLearner.Features.Courses;

public class CoursePatchDtoValidator : AbstractValidator<CoursePatchDto>
{
    public CoursePatchDtoValidator()
    {
        When(c => c.Name is not null, () =>
        {
            RuleFor(c => c.Name)
                .MinimumLength(10)
                .MaximumLength(30);
        });

        When(c => c.Description is not null, () =>
        {
            RuleFor(c => c.Description)
                .MinimumLength(10)
                .MaximumLength(200);
        });
    }
}