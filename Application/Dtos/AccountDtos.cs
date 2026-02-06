using Domain.Enums;

namespace Application.DTOs;

public record AccountDto(
    int Id,
    string AccountNumber,
    AccountType AccountType,
    decimal Balance,
    int UserId,
    AccountStatus Status,
    AccountLevel Level,
    DateTime CreatedAt);

public record CreateAccountDto(
    AccountType AccountType,
    int UserId,
    AccountLevel Level = AccountLevel.Level1);

public record UpdateAccountDto(
    AccountStatus Status,
    AccountLevel Level);
