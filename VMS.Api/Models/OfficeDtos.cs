using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Models;

public class OfficeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public OfficeStatus Status { get; set; }
}

public class CreateOfficeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public OfficeStatus Status { get; set; } = OfficeStatus.Active;
}

public class UpdateOfficeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public OfficeStatus Status { get; set; }
}
