using FluentValidation;
using FluentValidation.TestHelper;
using LinuxLearner.Features.CourseInvites;

namespace LinuxLearner.UnitTests;

public class CourseInvitePatchDtoValidatorTests
{
    private readonly IValidator<CourseInvitePatchDto> _validator = new CourseInvitePatchDtoValidator();

    [Theory]
    [InlineData(0), InlineData(1001)]
    public void ShouldReject_OutOfBoundsUsageLimit(int usageLimit)
    {
        var dto = new CourseInvitePatchDto(UsageLimit: usageLimit);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(i => i.UsageLimit);
    }

    [Theory]
    [InlineData(-1), InlineData(366)]
    public void ShouldReject_OutOfBoundsLifespan(int days)
    {
        var dto = new CourseInvitePatchDto(LifespanFromNow: TimeSpan.FromDays(days));
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(i => i.LifespanFromNow);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(10, null)]
    [InlineData(10, 10)]
    [InlineData(null, 10)]
    public void ShouldAccept_WithCorrectParameters(int? usageLimit, int? days)
    {
        var dto = new CourseInvitePatchDto(
            UsageLimit: usageLimit,
            LifespanFromNow: days.HasValue ? TimeSpan.FromDays(days.Value) : null);
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }
}