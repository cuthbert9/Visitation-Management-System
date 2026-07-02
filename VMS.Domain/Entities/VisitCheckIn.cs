namespace VisitorManagementSystem.Domain.Entities;

public class VisitCheckIn
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int CheckedInById { get; set; }
    public DateTime CheckInTime { get; set; }
    public string? Gate { get; set; }
    public string? BadgeNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public Visit Visit { get; set; } = null!;
    public User CheckedInBy { get; set; } = null!;
}
