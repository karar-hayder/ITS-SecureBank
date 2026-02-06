namespace Domain.Common;

public interface ISoftDeletableEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface IAuditableEntity
{
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
