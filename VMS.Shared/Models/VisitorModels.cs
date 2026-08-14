using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public enum IdentificationType
{
    NationalId = 1,
    Passport = 2,
    DriverLicense = 3,
    Other = 4
}

public class VisitorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Required]
    public IdentificationType IdentificationType { get; set; }

    [Required]
    public string IdentificationNumber { get; set; } = string.Empty;

    public string? Organization { get; set; }
    public string? PhotoUrl { get; set; }
}

public class UpdateVisitorDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    [Required]
    public IdentificationType IdentificationType { get; set; }

    [Required]
    public string IdentificationNumber { get; set; } = string.Empty;

    public string? Organization { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
