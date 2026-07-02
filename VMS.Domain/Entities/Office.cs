using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public string? OfficeCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
