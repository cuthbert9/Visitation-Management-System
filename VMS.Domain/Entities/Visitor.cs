namespace VisitorManagementSystem.Domain.Entities;

public class Visitor
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
