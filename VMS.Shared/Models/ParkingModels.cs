using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Shared.Models;

public enum ParkingSlotStatus
{
    Available = 1,
    Reserved = 2,
    Occupied = 3,
    Unavailable = 4
}

public class ParkingSlotDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public ParkingSlotStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
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

public class ReleaseParkingReservationDto
{
    public DateTime? ReleasedAt { get; set; }
}
