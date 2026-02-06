namespace Domain.Entities;

using Domain.Common;
using System;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = null!;
    public int EntityId { get; set; }
    public string Action { get; set; } = null!;
    public int? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Changes { get; set; }
}
