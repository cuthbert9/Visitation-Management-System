using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Entities;

public class ParkingSlot
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public ParkingSlotStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ParkingReservation> Reservations { get; set; } = new List<ParkingReservation>();
}
