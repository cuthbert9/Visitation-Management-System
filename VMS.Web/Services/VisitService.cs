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

    public async Task<IReadOnlyList<VisitStatusHistoryDto>> GetVisitHistoryAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<List<VisitStatusHistoryDto>>($"api/visits/{id}/history") ?? [];
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

    public async Task<VisitDto?> NotifyHostAsync(int id, VisitNotifyHostDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/notify-host", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> HostAcknowledgeAsync(int id, VisitHostAcknowledgeDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/host-acknowledge", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> DenyAsync(int id, VisitDenyDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/deny", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> MarkAttendedAsync(int id, VisitMarkAttendedDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/mark-attended", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> HostCompleteAsync(int id, VisitHostCompleteDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/host-complete", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CheckOutVisitAsync(int id, VisitCheckOutDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/checkout", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CloseAsync(int id, VisitCloseDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/close", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }

    public async Task<VisitDto?> CancelAsync(int id, VisitCancelDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visits/{id}/cancel", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }
}
