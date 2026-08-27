using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IVisitService
{
    Task<IReadOnlyList<VisitDto>> GetVisitsAsync();
    Task<IReadOnlyList<VisitDto>> GetGateRegisteredVisitsAsync();
    Task<VisitDto?> GetVisitByIdAsync(int id);
    Task<IReadOnlyList<VisitStatusHistoryDto>> GetVisitHistoryAsync(int id);
    Task<VisitDto?> CreateVisitAsync(VisitCreateDto request);
    Task<VisitDto?> CompleteHandoverAsync(int id, CompleteHandoverDto request);
    Task<VisitDto?> UpdateGateDetailsAsync(int id, UpdateGateDetailsDto request);
    Task<VisitDto?> NotifyHostAsync(int id, VisitNotifyHostDto request);
    Task<VisitDto?> HostAcknowledgeAsync(int id, VisitHostAcknowledgeDto request);
    Task<VisitDto?> DenyAsync(int id, VisitDenyDto request);
    Task<VisitDto?> MarkAttendedAsync(int id, VisitMarkAttendedDto request);
    Task<VisitDto?> HostCompleteAsync(int id, VisitHostCompleteDto request);
    Task<VisitDto?> CheckOutVisitAsync(int id, VisitCheckOutDto request);
    Task<VisitDto?> CloseAsync(int id, VisitCloseDto request);
    Task<VisitDto?> CancelAsync(int id, VisitCancelDto request);
}
