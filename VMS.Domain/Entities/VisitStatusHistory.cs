using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class VisitStatusHistory
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    public VisitStatus? FromStatus { get; set; }
    public VisitStatus ToStatus { get; set; }

    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Remarks { get; set; }

    public Visit Visit { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
