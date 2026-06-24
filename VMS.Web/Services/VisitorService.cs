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
}
