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

public record AccountResponseDto(
    int Id,
    string AccountNumber,
    AccountType AccountType,
    decimal Balance,
    AccountStatus Status,
    AccountLevel Level,
    DateTime CreatedAt);

public record CreateAccountDto(
    AccountType AccountType,
    AccountLevel Level = AccountLevel.Level1);

public record UpdateAccountDto(
    AccountStatus Status,
    AccountLevel Level);

public record DepositDto(decimal Amount);

public record WithdrawDto(decimal Amount);
