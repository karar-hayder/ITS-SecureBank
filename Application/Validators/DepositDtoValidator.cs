using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class DepositDtoValidator : AbstractValidator<DepositDto>
{
    public DepositDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Deposit amount must be greater than zero.")
            .Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Amount cannot have more than 2 decimal places.");
    }
}
