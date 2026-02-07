using Application.DTOs;
using static Application.DTOs.AuthDtos;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Backend.Tests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator;

    public RegisterDtoValidatorTests()
    {
        _validator = new RegisterDtoValidator();
    }

    [Theory]
    [InlineData("invalid-email")]
    public void Should_Have_Error_When_Email_Is_Invalid(string email)
    {
        // Order: FullName, Email, PhoneNumber, Password
        var model = new RegisterDto("User", email, "12345678", "Password123!");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nonumbers!")]
    [InlineData("NOLOWERCASE123!")]
    [InlineData("nouppercase123!")]
    public void Should_Have_Error_When_Password_Is_Weak(string password)
    {
        // Order: FullName, Email, PhoneNumber, Password
        var model = new RegisterDto("User", "test@example.com", "12345678", password);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        // FullName, Email, PhoneNumber, Password
        var model = new RegisterDto("Valid User", "valid@example.com", "12345678", "SecurePass123!");
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
