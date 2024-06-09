using FluentValidation;
using FluentValidation.TestHelper;
using LinuxLearner.Features.Courses;

namespace LinuxLearner.UnitTests;

public class CoursePatchDtoValidatorTests
{
    private readonly IValidator<CoursePatchDto> _validator = new CoursePatchDtoValidator();

    [Theory]
    [InlineData(9), InlineData(31)]
    public void ShouldReject_WithIncorrectNameLength(int nameLength)
    {
        var coursePatchDto = new CoursePatchDto(Name: new string('c', nameLength));
        _validator.TestValidate(coursePatchDto).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Theory]
    [InlineData(9), InlineData(201)]
    public void ShouldReject_WithIncorrectDescriptionLength(int descriptionLength)
    {
        var coursePatchDto = new CoursePatchDto(Description: new string('c', descriptionLength));
        _validator.TestValidate(coursePatchDto).ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("At least ten chars", null)]
    [InlineData(null, "At least ten chars")]
    [InlineData("At least ten chars", "At least ten chars")]
    [InlineData(null, null)]
    public void ShouldAccept_WithCorrectLengths(string? name, string? description)
    {
        var coursePatchDto = new CoursePatchDto(name, description);
        _validator.TestValidate(coursePatchDto).ShouldNotHaveAnyValidationErrors();
    }
}