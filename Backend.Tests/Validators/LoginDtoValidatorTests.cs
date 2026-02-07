using Application.DTOs;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;
using static Application.DTOs.AuthDtos;

namespace Backend.Tests.Validators;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator;

    public LoginDtoValidatorTests()
    {
        _validator = new LoginDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var model = new LoginDto("", "Password123!");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var model = new LoginDto("test@example.com", "");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
