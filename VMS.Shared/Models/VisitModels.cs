using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public enum VisitStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    CheckedIn = 4,
    CheckedOut = 5,
    Completed = 6,
    Cancelled = 7
}

public class VisitCheckInRecordDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int CheckedInById { get; set; }
    public DateTime CheckInTime { get; set; }
    public string? Gate { get; set; }
    public string? BadgeNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VisitCheckOutRecordDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int CheckedOutById { get; set; }
    public DateTime CheckOutTime { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VisitDto
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public int OfficeId { get; set; }
    public int? CreatedById { get; set; }
    public int? ApprovedById { get; set; }
    public DateTime VisitDate { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public VisitStatus Status { get; set; }
    public string? Purpose { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public VisitCheckInRecordDto? LatestCheckIn { get; set; }
    public VisitCheckOutRecordDto? LatestCheckOut { get; set; }
    public List<VisitParkingReservationDto> ParkingReservations { get; set; } = [];
    public VisitorDto Visitor { get; set; } = null!;
    public OfficeDto Office { get; set; } = null!;
}

public class VisitParkingReservationDto
{
    public int Id { get; set; }
    public int SlotId { get; set; }
    public string SlotCode { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public string? VehiclePlate { get; set; }
    public ParkingReservationStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
}

public enum ParkingReservationStatus
{
    Pending = 1,
    Reserved = 2,
    Released = 3,
    Cancelled = 4
}

public class VisitCreateDto
{
    [Required]
    public int VisitorId { get; set; }

    [Required]
    public int OfficeId { get; set; }

    public int? CreatedById { get; set; }

    [Required]
    public DateTime VisitDate { get; set; }

    public DateTime? ExpectedArrival { get; set; }
    public string? Purpose { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class VisitApprovalDto
{
    [Required]
    public int ApprovedById { get; set; }
}

public class VisitCheckInDto
{
    [Required]
    public int CheckedInById { get; set; }

    public string? Gate { get; set; }
    public string? BadgeNumber { get; set; }
}

public class VisitCheckOutDto
{
    [Required]
    public int CheckedOutById { get; set; }

    public string? Remarks { get; set; }
}
