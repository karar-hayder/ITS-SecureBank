namespace Domain.Entities;

using Domain.Common;
using Domain.Enums;

public class User : BaseEntity, IAuditableEntity, ISoftDeletableEntity
{
    public required string FullName { get; set; }
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole userRole { get; set; } = UserRole.User;
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
