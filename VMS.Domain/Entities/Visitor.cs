using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Visitor
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
