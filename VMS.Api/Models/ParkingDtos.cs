using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Api.Models;

public class ParkingSlotDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public ParkingSlotStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateParkingSlotDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    public string? Zone { get; set; }
    public ParkingSlotStatus Status { get; set; } = ParkingSlotStatus.Available;
}

public class UpdateParkingSlotDto
{
    [Required]
    public string Code { get; set; } = string.Empty;

    public string? Zone { get; set; }
    public ParkingSlotStatus Status { get; set; }
}

public class ParkingReservationDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int SlotId { get; set; }
    public string? VehiclePlate { get; set; }
    public ParkingReservationStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateParkingReservationDto
{
    [Required]
    public int VisitId { get; set; }

    [Required]
    public int SlotId { get; set; }

    public string? VehiclePlate { get; set; }
}

public class UpdateParkingReservationDto
{
    [Required]
    public int SlotId { get; set; }

    public string? VehiclePlate { get; set; }
    public ParkingReservationStatus Status { get; set; }
}

public class ReleaseParkingReservationDto
{
    public DateTime? ReleasedAt { get; set; }
}

public class CancelParkingReservationDto
{
}
