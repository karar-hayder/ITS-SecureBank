using System;
using Domain.Common;
using Domain.Enums;


namespace Domain.Entities;


public class TransferIntents : BaseEntity, IAuditableEntity, ISoftDeletableEntity
{
    public string TransferIntentId { get; set; } = default!;
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal? Amount { get; set; }
    public TransferIntentStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Account? FromAccount { get; set; }
    public virtual Account? ToAccount { get; set; }
}