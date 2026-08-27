using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Models;

public class VisitItemDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVisitItemDto
{
    [Required]
    public int VisitId { get; set; }

    [Required]
    public string ItemName { get; set; } = string.Empty;

    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Remarks { get; set; }
}

public class UpdateVisitItemDto
{
    [Required]
    public string ItemName { get; set; } = string.Empty;

    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Remarks { get; set; }
}
