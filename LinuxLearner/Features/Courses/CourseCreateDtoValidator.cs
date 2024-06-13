using FluentValidation;
using LinuxLearner.Domain;

namespace LinuxLearner.Features.Courses;

public class CourseCreateDtoValidator : AbstractValidator<CourseCreateDto>
{
    public CourseCreateDtoValidator()
    {
        RuleFor(c => c.AcceptanceMode)
            .NotEqual(AcceptanceMode.Closed)
            .WithMessage("A course cannot start out as closed");

        RuleFor(c => c.Name)
            .MinimumLength(10)
            .MaximumLength(30);

        When(c => c.Description is not null, () =>
        {
            RuleFor(c => c.Description)
                .MinimumLength(10)
                .MaximumLength(200);
        });
    }
}