using Application.DTOs;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Backend.Tests.Validators;

public class AccountValidatorsTests
{
    [Fact]
    public void DepositDtoValidator_Should_Fail_On_Zero_Amount()
    {
        var validator = new DepositDtoValidator();
        var model = new DepositDto(0m);
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void WithdrawDtoValidator_Should_Fail_On_Negative_Amount()
    {
        var validator = new WithdrawDtoValidator();
        var model = new WithdrawDto(-10m);
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void CreateAccountDtoValidator_Should_Fail_On_Invalid_Type()
    {
        var validator = new CreateAccountDtoValidator();
        var model = new CreateAccountDto((Domain.Enums.AccountType)99);
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AccountType);
    }
}
