using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class VisitItem
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public Visit Visit { get; set; } = null!;
}
