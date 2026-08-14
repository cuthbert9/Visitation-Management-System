using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class Visit
{
    public int Id { get; set; }
    public string VisitNumber { get; set; } = string.Empty;

    public int VisitorId { get; set; }
    public int HostEmployeeId { get; set; }
    public int DepartmentId { get; set; }

    public VisitPurposeType Purpose { get; set; }
    public string? PurposeDescription { get; set; }

    public VisitStatus Status { get; set; }
    public StaffAvailabilityStatus? StaffAvailabilityStatus { get; set; }

    public DateTime ArrivalTime { get; set; }
    public DateTime? ExpectedDepartureTime { get; set; }
    public DateTime? DepartureTime { get; set; }

    public string? VehicleModel { get; set; }
    public string? VehiclePlateNumber { get; set; }

    public string? BadgeNumber { get; set; }
    public BadgeStatus? BadgeStatus { get; set; }
    public DateTime? BadgeIssuedAt { get; set; }
    public DateTime? BadgeReturnedAt { get; set; }

    public DateTime? HostAcknowledgedAt { get; set; }
    public DateTime? HostCompletedAt { get; set; }
    public DateTime? VisitorExitAcknowledgedAt { get; set; }
    public string? ExitSignatureReference { get; set; }

    public int CheckedInById { get; set; }
    public int? CheckedOutById { get; set; }

    public string? Remarks { get; set; }
    public string? AttachmentUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Visitor Visitor { get; set; } = null!;
    public Employee HostEmployee { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public User CheckedInBy { get; set; } = null!;
    public User? CheckedOutBy { get; set; }

    public ICollection<VisitItem> Items { get; set; } = new List<VisitItem>();
    public ICollection<VisitStatusHistory> StatusHistory { get; set; } = new List<VisitStatusHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ParkingReservation> ParkingReservations { get; set; } = new List<ParkingReservation>();
}
