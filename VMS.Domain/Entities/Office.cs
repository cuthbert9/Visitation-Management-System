using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public OfficeStatus Status { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
