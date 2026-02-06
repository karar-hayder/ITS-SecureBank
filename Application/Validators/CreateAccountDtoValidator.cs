using FluentValidation;
using Application.DTOs;
using Domain.Enums;

namespace Application.Validators;

public class CreateAccountDtoValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountDtoValidator()
    {
        RuleFor(x => x.AccountType)
            .IsInEnum()
            .WithMessage("Invalid account type. Must be Checking or Savings.");

        RuleFor(x => x.Level)
            .IsInEnum()
            .WithMessage("Invalid account level. Must be Level1, Level2, or Level3.");
    }
}
