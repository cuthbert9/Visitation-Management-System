using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitorService
{
    Task<IReadOnlyList<VisitorDto>> GetVisitorsAsync();
    Task<VisitorDto?> GetVisitorByIdAsync(int id);
    Task<VisitorDto?> CreateVisitorAsync(CreateVisitorDto request);
}
