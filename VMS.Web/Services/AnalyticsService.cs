using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class AnalyticsService(HttpClient httpClient) : IAnalyticsService
{
    public async Task<VisitsDashboardDto> GetVisitsDashboardAsync()
    {
        return await httpClient.GetFromJsonAsync<VisitsDashboardDto>("api/analytics/visits-dashboard")
            ?? new VisitsDashboardDto();
    }
}
