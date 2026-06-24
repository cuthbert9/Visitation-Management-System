namespace VisitorManagementSystem.Shared.Models;

public enum OfficeStatus
{
    Active = 1,
    Inactive = 2
}

public class OfficeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public OfficeStatus Status { get; set; }
}
