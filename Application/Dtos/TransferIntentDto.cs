namespace Application.DTOs;
using Domain.Enums;
public class TransferIntentDto
{
    public string Id { get; set; } = default!;
    public string FromAccountId { get; set; } = default!;
    public string ToAccountId { get; set; } = default!;
    public decimal Amount { get; set; }
    public TransferIntentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}


public class CreateTransferIntentDto
{
    public string FromAccountNumber { get; set; } = default!;
    public string ToAccountNumber { get; set; } = default!;
}

public class CompleteTransferDto
{
    public string TransferIntentId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = default!;
}

public class CancelTransferIntentDto
{
    public string TransferIntentId { get; set; } = default!;
}

