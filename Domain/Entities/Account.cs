namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;

public class Account : BaseEntity, IAuditableEntity, ISoftDeletableEntity
{
    public required string AccountNumber { get; set; }
    public required AccountType AccountType { get; set; }
    public decimal Balance { get; set; } = 0m;
    public int UserId { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public AccountLevel Level { get; set; } = AccountLevel.Level1;
    public byte[] RowVersion { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
