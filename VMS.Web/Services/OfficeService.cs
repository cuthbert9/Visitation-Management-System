using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class OfficeService(HttpClient httpClient) : IOfficeService
{
    public async Task<IReadOnlyList<OfficeDto>> GetOfficesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<OfficeDto>>("api/offices") ?? [];
    }
}
