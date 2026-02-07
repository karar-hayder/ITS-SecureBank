using Domain.Enums;

namespace Application.DTOs;

public record TransactionDto(
    int Id,
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    string ReferenceId,
    string? Description,
    DateTime CreatedAt,
    int? RelatedAccountId);

public record TransactionResponseDto(
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    string ReferenceId,
    string? Description,
    DateTime CreatedAt);