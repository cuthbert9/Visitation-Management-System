using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Visit
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public int OfficeId { get; set; }
    public DateTime VisitDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public VisitStatus Status { get; set; }
    public string? Purpose { get; set; }
    public DateTime CreatedAt { get; set; }

    public Visitor Visitor { get; set; } = null!;
    public Office Office { get; set; } = null!;
}


