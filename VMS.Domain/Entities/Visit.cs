using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Visit
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public int OfficeId { get; set; }
    public string? Purpose { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime VisitDate { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public VisitStatus Status { get; set; }
    public int? CreatedById { get; set; }
    public int? ApprovedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Visitor Visitor { get; set; } = null!;
    public Office Office { get; set; } = null!;
    public User? CreatedBy { get; set; }
    public User? ApprovedBy { get; set; }
    public ICollection<VisitCheckIn> CheckIns { get; set; } = new List<VisitCheckIn>();
    public ICollection<VisitCheckOut> CheckOuts { get; set; } = new List<VisitCheckOut>();
    public ICollection<ParkingReservation> ParkingReservations { get; set; } = new List<ParkingReservation>();
}
