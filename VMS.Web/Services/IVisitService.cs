using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitService
{
    Task<IReadOnlyList<VisitDto>> GetVisitsAsync();
    Task<VisitDto?> GetVisitByIdAsync(int id);
    Task<VisitDto?> CreateVisitAsync(VisitCreateDto request);
    Task<VisitDto?> ApproveVisitAsync(int id);
    Task<VisitDto?> RejectVisitAsync(int id);
    Task<VisitDto?> CheckInVisitAsync(int id);
    Task<VisitDto?> CheckOutVisitAsync(int id);
}
