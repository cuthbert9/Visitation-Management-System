using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public enum VisitStatus
{
    GateRegistered = 0,
    Registered = 1,
    WaitingForHost = 2,
    HostAcknowledged = 3,
    Attended = 4,
    AwaitingExit = 5,
    Completed = 6,
    Closed = 7,
    Cancelled = 8,
    Denied = 9
}

public enum StaffAvailabilityStatus
{
    Unknown = 1,
    Available = 2,
    Busy = 3,
    Unavailable = 4,
    OutOfOffice = 5
}

public enum VisitPurposeType
{
    OfficialMeeting = 1,
    Meeting = 1,
    Interview = 2,
    Delivery = 3,
    Maintenance = 4,
    Official = 5,
    OfficialBusiness = 5,
    Personal = 6,
    Facilitator = 7,
    Training = 7,
    Technician = 8,
    ContractorWork = 8,
    Other = 9,
    ExternalAuditor = 10
}

public enum BadgeStatus
{
    Available = 1,
    Issued = 2,
    Lost = 3,
    Damaged = 4,
    Disabled = 5
}

public enum VisitItemType
{
    Personal = 1,
    Equipment = 2,
    Delivery = 3,
    Tool = 4,
    ElectronicDevice = 5,
    Document = 6,
    Other = 7
}

public enum ItemMovementType
{
    BroughtIn = 1,
    Delivered = 2,
    TakenOut = 3
}

public enum ParkingReservationStatus
{
    Pending = 1,
    Reserved = 2,
    Released = 3,
    Cancelled = 4
}

public class VisitItemDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVisitItemDto
{
    [Required]
    public int VisitId { get; set; }

    [Required]
    public string ItemName { get; set; } = string.Empty;

    public VisitItemType? ItemType { get; set; }
    public ItemMovementType? MovementType { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Remarks { get; set; }
}

public class VisitStatusHistoryDto
{
    public int Id { get; set; }
    public VisitStatus? FromStatus { get; set; }
    public VisitStatus ToStatus { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Remarks { get; set; }
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

public class VisitDto
{
    public int Id { get; set; }
    public string VisitNumber { get; set; } = string.Empty;

    public int VisitorId { get; set; }
    public int? HostEmployeeId { get; set; }
    public int? DepartmentId { get; set; }

    public VisitPurposeType? Purpose { get; set; }
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

    public List<VisitParkingReservationDto> ParkingReservations { get; set; } = [];
    public List<VisitItemDto> Items { get; set; } = [];

    public VisitorDto Visitor { get; set; } = null!;
    public EmployeeDto? HostEmployee { get; set; }
    public DepartmentDto? Department { get; set; }
    public VisitEquipmentDto? Equipment { get; set; }
}

public class VisitCreateDto
{
    [Required]
    public int VisitorId { get; set; }

    public int? HostEmployeeId { get; set; }
    public int? DepartmentId { get; set; }

    [Required]
    public int CheckedInById { get; set; }

    public VisitPurposeType? Purpose { get; set; }

    public string? PurposeDescription { get; set; }
    public DateTime? ExpectedDepartureTime { get; set; }
    public string? VehicleModel { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? BadgeNumber { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class CompleteHandoverDto
{
    [Required]
    public int HostEmployeeId { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public VisitPurposeType Purpose { get; set; }

    public string? PurposeDescription { get; set; }
    public string? BadgeNumber { get; set; }
}

public class UpdateGateDetailsDto
{
    public string? VehicleModel { get; set; }
    public string? VehiclePlateNumber { get; set; }
}

public class VisitEquipmentDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public bool HasLaptop { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceBrand { get; set; }
    public string? AssetTag { get; set; }
    public bool PcConfirmedReturned { get; set; }
}

public class CreateVisitEquipmentDto
{
    [Required]
    public int VisitId { get; set; }

    public bool HasLaptop { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceBrand { get; set; }
    public string? AssetTag { get; set; }
}

public class VisitNotifyHostDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}

public class VisitHostAcknowledgeDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public StaffAvailabilityStatus? StaffAvailabilityStatus { get; set; }
    public string? Remarks { get; set; }
}

public class VisitDenyDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}

public class VisitMarkAttendedDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}

public class VisitHostCompleteDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}

public class VisitCheckOutDto
{
    [Required]
    public int CheckedOutById { get; set; }

    public bool BadgeReturned { get; set; }
    public string? ExitSignatureReference { get; set; }
    public string? Remarks { get; set; }
}

public class VisitCloseDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}

public class VisitCancelDto
{
    [Required]
    public int ChangedByUserId { get; set; }

    public string? Remarks { get; set; }
}
