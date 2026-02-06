namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;

public class Account : BaseEntity
{
    public required string AccountNumber { get; set; } // Unique 10-digit account number
    public required AccountType AccountType { get; set; }
    public decimal Balance { get; set; } = 0m; // Using decimal for financial precision
    public int UserId { get; set; }
    
    // Concurrency token for optimistic locking to prevent race conditions
    public byte[] RowVersion { get; set; } = null!;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
