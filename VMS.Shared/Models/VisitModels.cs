using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public enum VisitStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4
}

public class VisitDto
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
    public VisitorDto Visitor { get; set; } = null!;
    public OfficeDto Office { get; set; } = null!;
}

public class VisitCreateDto
{
    [Required]
    public int VisitorId { get; set; }

    [Required]
    public int OfficeId { get; set; }

    [Required]
    public DateTime VisitDate { get; set; }

    public string? Purpose { get; set; }
}
