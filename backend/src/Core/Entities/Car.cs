namespace Core.Entities;

public class Car : AuditableEntity
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Vin { get; set; } = string.Empty;
    
    public virtual ICollection<OwnerCar> OwnerCars { get; set; } = new List<OwnerCar>();
}