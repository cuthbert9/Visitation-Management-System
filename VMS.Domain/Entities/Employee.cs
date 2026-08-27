namespace VisitorManagementSystem.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public int DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Department Department { get; set; } = null!;
    public ICollection<Visit> HostedVisits { get; set; } = new List<Visit>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
