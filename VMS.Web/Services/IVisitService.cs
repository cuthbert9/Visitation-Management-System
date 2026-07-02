using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitService
{
    Task<IReadOnlyList<VisitDto>> GetVisitsAsync();
    Task<VisitDto?> GetVisitByIdAsync(int id);
    Task<VisitDto?> CreateVisitAsync(VisitCreateDto request);
    Task<VisitDto?> ApproveVisitAsync(int id, VisitApprovalDto request);
    Task<VisitDto?> RejectVisitAsync(int id, VisitApprovalDto request);
    Task<VisitDto?> CheckInVisitAsync(int id, VisitCheckInDto request);
    Task<VisitDto?> CheckOutVisitAsync(int id, VisitCheckOutDto request);
}
