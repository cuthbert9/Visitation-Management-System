using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public class VisitorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? NationalId { get; set; }
    public string? Company { get; set; }
    public string? VehiclePlate { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? NationalId { get; set; }
    public string? Company { get; set; }
    public string? VehiclePlate { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? NationalId { get; set; }
    public string? Company { get; set; }
    public string? VehiclePlate { get; set; }
    public string? PhotoUrl { get; set; }
}
