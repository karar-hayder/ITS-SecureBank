namespace Domain.Common;

public interface ISoftDeletable
{
    public bool IsDeleted { get; set; }
}

public interface IAuditable
{
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}
