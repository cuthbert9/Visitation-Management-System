using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class VisitService(HttpClient httpClient) : IVisitService
{
    public async Task<IReadOnlyList<VisitDto>> GetVisitsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<VisitDto>>("api/visits") ?? [];
    }

    public async Task<VisitDto?> CreateVisitAsync(VisitCreateDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/visits", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitDto>();
    }
}
