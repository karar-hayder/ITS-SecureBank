namespace Application.Common.Models;

public class TransferRequest
{
    public required string FromAccountId { get; set; }
    public required string ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; } // For idempotency
}

public class TransferResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ReferenceId { get; set; }
}
