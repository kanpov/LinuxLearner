using FluentValidation;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInviteCreateDtoValidator : AbstractValidator<CourseInviteCreateDto>
{
    public CourseInviteCreateDtoValidator()
    {
        RuleFor(i => i.UsageLimit)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(1000);

        When(i => i.Lifespan is not null, () =>
        {
            RuleFor(i => i.Lifespan)
                .GreaterThan(TimeSpan.Zero)
                .WithMessage("Lifespan cannot be zero or negative")
                .LessThanOrEqualTo(TimeSpan.FromDays(365))
                .WithMessage("Lifespan cannot exceed 1 year");
        });
    }
}