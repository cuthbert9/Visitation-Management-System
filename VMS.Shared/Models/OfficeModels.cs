using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public class OfficeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public string? OfficeCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOfficeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Floor { get; set; }
    public string? OfficeCode { get; set; }
}

public class UpdateOfficeDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Floor { get; set; }
    public string? OfficeCode { get; set; }
}
