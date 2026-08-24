namespace VisitorManagementSystem.Domain.Entities;

public class VisitEquipment
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    public bool HasLaptop { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceBrand { get; set; }
    public string? AssetTag { get; set; }
    public bool PcConfirmedReturned { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Visit Visit { get; set; } = null!;
}
