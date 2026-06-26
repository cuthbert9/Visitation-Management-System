using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class OfficeService(HttpClient httpClient) : IOfficeService
{
    public async Task<IReadOnlyList<OfficeDto>> GetOfficesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<OfficeDto>>("api/offices") ?? [];
    }

    public async Task<OfficeDto?> GetOfficeByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<OfficeDto>($"api/offices/{id}");
    }

    public async Task<OfficeDto?> CreateOfficeAsync(CreateOfficeDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/offices", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OfficeDto>();
    }

    public async Task<OfficeDto?> UpdateOfficeAsync(int id, UpdateOfficeDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/offices/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OfficeDto>();
    }

    public async Task DeleteOfficeAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/offices/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VisitDto>> GetOfficeVisitsAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<List<VisitDto>>($"api/offices/{id}/visits") ?? [];
    }
}
