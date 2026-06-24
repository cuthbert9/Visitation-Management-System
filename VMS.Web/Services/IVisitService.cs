using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitService
{
    Task<IReadOnlyList<VisitDto>> GetVisitsAsync();
    Task<VisitDto?> CreateVisitAsync(VisitCreateDto request);
}
