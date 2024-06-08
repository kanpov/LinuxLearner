using FluentValidation;
using FluentValidation.TestHelper;
using LinuxLearner.Features.Users;

namespace LinuxLearner.UnitTests;

public class UserPatchDtoValidatorTests
{
    private readonly IValidator<UserPatchDto> _validator = new UserPatchDtoValidator();

    [Theory]
    [InlineData(null), InlineData("abcdeabcdeabcde")]
    public void ShouldAccept_WithCorrectDescription(string? description)
    {
        var userPatchDto = new UserPatchDto(description);
        _validator.TestValidate(userPatchDto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(9), InlineData(101)]
    public void ShouldReject_WithIncorrectDescriptionLength(int length)
    {
        var userPatchDto = new UserPatchDto(new string('c', length));
        _validator.TestValidate(userPatchDto).ShouldHaveValidationErrorFor(c => c.Description);
    }
}