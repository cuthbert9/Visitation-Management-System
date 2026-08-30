using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Models;

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
    public string? AttachmentUrl { get; set; }
}

public class UpdateGateDetailsDto
{
    public string? VehicleModel { get; set; }
    public string? VehiclePlateNumber { get; set; }
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
