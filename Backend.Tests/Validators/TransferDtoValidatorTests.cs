using Application.DTOs;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Backend.Tests.Validators;

public class TransferDtoValidatorTests
{
    private readonly TransferDtoValidator _validator;

    public TransferDtoValidatorTests()
    {
        _validator = new TransferDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Amount_Is_Zero_Or_Negative()
    {
        var model = new TransferDto("ACC1", "ACC2", 0m, "Test");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Should_Have_Error_When_From_Equals_To()
    {
        var model = new TransferDto("ACC1", "ACC1", 100m, "Test");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Source and destination account numbers cannot be the same.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        var model = new TransferDto("ACC1", "ACC2", 100m, "Test");
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
