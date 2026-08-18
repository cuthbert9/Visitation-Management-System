using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class ParkingService(HttpClient httpClient) : IParkingService
{
    public async Task<IReadOnlyList<ParkingSlotDto>> GetSlotsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ParkingSlotDto>>("api/parkingslots") ?? [];
    }

    public async Task<IReadOnlyList<ParkingReservationDto>> GetReservationsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ParkingReservationDto>>("api/parkingreservations") ?? [];
    }

    public async Task<ParkingReservationDto?> CreateReservationAsync(CreateParkingReservationDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/parkingreservations", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ParkingReservationDto>();
    }

    public async Task<ParkingReservationDto?> ReleaseReservationAsync(int reservationId, ReleaseParkingReservationDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/parkingreservations/{reservationId}/release", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ParkingReservationDto>();
    }
}
