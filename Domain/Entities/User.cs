namespace Domain.Entities;

using Domain.Common;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<int>, IAuditableEntity, ISoftDeletableEntity
{
    public required string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
