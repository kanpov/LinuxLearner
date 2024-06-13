using FluentValidation;
using FluentValidation.TestHelper;
using LinuxLearner.Features.CourseInvites;

namespace LinuxLearner.UnitTests;

public class CourseInviteCreateDtoValidatorTests
{
    private readonly IValidator<CourseInviteCreateDto> _validator = new CourseInviteCreateDtoValidator();

    [Theory]
    [InlineData(0), InlineData(1001)]
    public void ShouldReject_OutOfBoundsUsageLimit(int usageLimit)
    {
        var dto = new CourseInviteCreateDto(usageLimit);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(i => i.UsageLimit);
    }

    [Theory]
    [InlineData(-1), InlineData(366)]
    public void ShouldReject_OutOfBoundsLifespan(int days)
    {
        var dto = new CourseInviteCreateDto(10, TimeSpan.FromDays(days));
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(i => i.Lifespan);
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(10, null)]
    public void ShouldAccept_WithCorrectParameters(int usageLimit, int? days)
    {
        var dto = new CourseInviteCreateDto(usageLimit, days.HasValue ? TimeSpan.FromDays(days.Value) : null);
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }
}