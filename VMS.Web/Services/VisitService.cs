using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class VisitService(HttpClient httpClient) : IVisitService
{
    public async Task<IReadOnlyList<VisitDto>> GetVisitsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<VisitDto>>("api/visits") ?? [];
    }

    public async Task<VisitDto?> GetVisitByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<VisitDto>($"api/visits/{id}");
    }

    public async Task<VisitDto?> CreateVisitAsync(VisitCreateDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/visits", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> ApproveVisitAsync(int id)
    {
        var response = await httpClient.PutAsync($"api/visits/{id}/approve", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> RejectVisitAsync(int id)
    {
        var response = await httpClient.PutAsync($"api/visits/{id}/reject", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CheckInVisitAsync(int id)
    {
        var response = await httpClient.PutAsync($"api/visits/{id}/checkin", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CheckOutVisitAsync(int id)
    {
        var response = await httpClient.PutAsync($"api/visits/{id}/checkout", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }
}
