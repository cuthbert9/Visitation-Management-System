using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IParkingService
{
    Task<IReadOnlyList<ParkingSlotDto>> GetSlotsAsync();
    Task<IReadOnlyList<ParkingReservationDto>> GetReservationsAsync();
    Task<ParkingReservationDto?> CreateReservationAsync(CreateParkingReservationDto request);
    Task<ParkingReservationDto?> ReleaseReservationAsync(int reservationId, ReleaseParkingReservationDto request);
}
