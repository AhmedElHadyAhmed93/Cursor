namespace Core.Entities;

public enum AuditMode
{
    None,
    CreateOnly,
    UpdateOnly,
    DeleteOnly,
    All
}

public abstract class BaseEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
    public AuditMode AuditMode { get; set; } = AuditMode.All;
}