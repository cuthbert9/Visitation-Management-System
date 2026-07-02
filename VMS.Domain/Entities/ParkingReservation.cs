using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class ParkingReservation
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int SlotId { get; set; }
    public string? VehiclePlate { get; set; }
    public ParkingReservationStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Visit Visit { get; set; } = null!;
    public ParkingSlot Slot { get; set; } = null!;
}
