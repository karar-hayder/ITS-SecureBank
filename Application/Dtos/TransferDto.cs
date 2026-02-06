namespace Application.DTOs;

public record TransferDto(
    string FromAccountNumber,
    string ToAccountNumber,
    decimal Amount,
    string? Description);
