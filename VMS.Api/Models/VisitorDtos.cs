using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Models;

public class VisitorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
}

public class UpdateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
}
