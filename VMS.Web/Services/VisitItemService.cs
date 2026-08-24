using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class VisitItemService(HttpClient httpClient) : IVisitItemService
{
    public async Task<IReadOnlyList<VisitItemDto>> GetByVisitAsync(int visitId)
    {
        return await httpClient.GetFromJsonAsync<List<VisitItemDto>>($"api/visititems/visit/{visitId}") ?? [];
    }

    public async Task<VisitItemDto?> CreateAsync(CreateVisitItemDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/visititems", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitItemDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/visititems/{id}");
        response.EnsureSuccessStatusCode();
    }
}
