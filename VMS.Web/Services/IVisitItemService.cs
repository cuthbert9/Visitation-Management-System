using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitItemService
{
    Task<IReadOnlyList<VisitItemDto>> GetByVisitAsync(int visitId);
    Task<VisitItemDto?> CreateAsync(CreateVisitItemDto request);
    Task DeleteAsync(int id);
}
