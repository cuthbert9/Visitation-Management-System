using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IAnalyticsService
{
    Task<VisitsDashboardDto> GetVisitsDashboardAsync();
}
