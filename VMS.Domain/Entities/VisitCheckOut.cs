namespace VisitorManagementSystem.Domain.Entities;

public class VisitCheckOut
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int CheckedOutById { get; set; }
    public DateTime CheckOutTime { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }

    public Visit Visit { get; set; } = null!;
    public User CheckedOutBy { get; set; } = null!;
}
