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
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(string.IsNullOrWhiteSpace(error)
                ? $"Request failed with status code {(int)response.StatusCode}."
                : error);
        }

        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> ApproveVisitAsync(int id, VisitApprovalDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/approve", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> RejectVisitAsync(int id, VisitApprovalDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/reject", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CheckInVisitAsync(int id, VisitCheckInDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/checkin", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CheckOutVisitAsync(int id, VisitCheckOutDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/checkout", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }
}
