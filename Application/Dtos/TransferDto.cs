namespace Application.DTOs;

public record TransferDto(
    int FromAccountId,
    string ToAccountNumber,
    decimal Amount,
    string? Description);
