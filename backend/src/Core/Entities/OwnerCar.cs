namespace Core.Entities;

public class OwnerCar : AuditableEntity
{
    public int CarId { get; set; }
    public virtual Car Car { get; set; } = null!;
    
    public string OwnerId { get; set; } = string.Empty;
    
    public string OwnershipType { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}