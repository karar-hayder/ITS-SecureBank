using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class AccountApprovalRequestDtoValidator : AbstractValidator<AccountApprovalRequestDto>
{
    public AccountApprovalRequestDtoValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .WithMessage("Valid Account ID is required.");

        RuleFor(x => x.IdDocument)
            .NotNull()
            .WithMessage("ID Document is required.");
    }
}
