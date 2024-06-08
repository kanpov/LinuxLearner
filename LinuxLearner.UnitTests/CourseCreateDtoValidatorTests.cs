using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.TestHelper;
using LinuxLearner.Domain;
using LinuxLearner.Features.Courses;

namespace LinuxLearner.UnitTests;

public class CourseCreateDtoValidatorTests
{
    private readonly IValidator<CourseCreateDto> _validator = new CourseCreateDtoValidator();

    [Theory, AutoData]
    public void ShouldReject_WithClosedAcceptanceMode(CourseCreateDto courseCreateDto)
    {
        courseCreateDto = courseCreateDto with { AcceptanceMode = AcceptanceMode.Closed };
        _validator.TestValidate(courseCreateDto).ShouldHaveValidationErrorFor(c => c.AcceptanceMode);
    }

    [Theory]
    [InlineData(9), InlineData(31)]
    public void ShouldReject_WithIncorrectNameLength(int nameLength)
    {
        var courseCreateDto =
            new CourseCreateDto(new string('c', nameLength), Description: null, AcceptanceMode.InviteRequired);
        _validator.TestValidate(courseCreateDto).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Theory]
    [InlineData(9), InlineData(201)]
    public void ShouldReject_WithIncorrectDescriptionLength(int descriptionLength)
    {
        var courseCreateDto =
            new CourseCreateDto("name", new string('c', descriptionLength), AcceptanceMode.InviteRequired);
        _validator.TestValidate(courseCreateDto).ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(null), InlineData("Description with at least 10 characters")]
    public void ShouldAccept_WithCorrectParameters(string? description)
    {
        var courseCreateDto = new CourseCreateDto(new string('c', 15), description, AcceptanceMode.InviteRequired);
        _validator.TestValidate(courseCreateDto).ShouldNotHaveAnyValidationErrors();
    }
}