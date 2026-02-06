namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<int>, IAuditableEntity, ISoftDeletableEntity
{
    public required string FullName { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole UserRole { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
