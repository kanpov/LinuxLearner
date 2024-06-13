using FluentValidation;

namespace LinuxLearner.Features.CourseInvites;

public class CourseInvitePatchDtoValidator : AbstractValidator<CourseInvitePatchDto>
{
    public CourseInvitePatchDtoValidator()
    {
        When(i => i.UsageLimit is not null, () =>
        {
            RuleFor(i => i.UsageLimit)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(1000);
        });

        When(i => i.LifespanFromNow is not null, () =>
        {
            RuleFor(i => i.LifespanFromNow)
                .GreaterThan(TimeSpan.Zero)
                .WithMessage("New lifespan must be greater than zero")
                .LessThanOrEqualTo(TimeSpan.FromDays(365))
                .WithMessage("New lifespan must be up to 1 year");
        });
    }
}