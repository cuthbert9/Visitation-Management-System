using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class VisitorService(HttpClient httpClient) : IVisitorService
{
    public async Task<IReadOnlyList<VisitorDto>> GetVisitorsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<VisitorDto>>("api/visitors") ?? [];
    }

    public async Task<VisitorDto?> GetVisitorByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<VisitorDto>($"api/visitors/{id}");
    }

    public async Task<VisitorDto?> CreateVisitorAsync(CreateVisitorDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/visitors", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitorDto>();
    }

    public async Task<VisitorDto?> UpdateVisitorAsync(int id, UpdateVisitorDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/visitors/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitorDto>();
    }

    public async Task DeleteVisitorAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/visitors/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VisitDto>> GetVisitorVisitsAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<List<VisitDto>>($"api/visitors/{id}/visits") ?? [];
    }
}
