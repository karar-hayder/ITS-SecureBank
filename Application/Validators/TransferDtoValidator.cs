using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public class TransferDtoValidator : AbstractValidator<TransferDto>
{
    public TransferDtoValidator()
    {
        RuleFor(x => x.FromAccountNumber)
            .NotEmpty()
            .WithMessage("Source account number is required.")
            .MaximumLength(34)
            .WithMessage("Account number cannot exceed 34 characters.");

        RuleFor(x => x.ToAccountNumber)
            .NotEmpty()
            .WithMessage("Destination account number is required.")
            .MaximumLength(34)
            .WithMessage("Account number cannot exceed 34 characters.");

        RuleFor(x => x)
            .Must(x => x.FromAccountNumber != x.ToAccountNumber)
            .WithMessage("Source and destination account numbers cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Transfer amount must be greater than zero.")
            .Must(amount => decimal.Round(amount, 2) == amount)
            .WithMessage("Amount cannot have more than 2 decimal places.");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters.");
    }
}
