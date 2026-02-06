namespace Domain.Entities;

using Domain.Common;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser<int>
{
    public required string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
