using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IOutboundVisitService
{
    Task<IReadOnlyList<OutboundVisitDto>> GetVisitsAsync();
    Task<OutboundVisitDto?> GetVisitAsync(string id);
    Task<OutboundVisitDto> SaveDraftAsync(PlanOutboundVisitRequest request);
    Task<OutboundVisitDto> SubmitForApprovalAsync(PlanOutboundVisitRequest request);
    Task<OutboundVisitDto> UpdateDetailsAsync(PlanOutboundVisitRequest request);
    Task<OutboundVisitDto> DecideApprovalAsync(string id, ApprovalDecisionRequest request);
    Task<OutboundVisitDto> RescheduleAsync(string id, DateOnly proposedDate, TimeOnly startTime, TimeOnly? endTime);
    Task<OutboundVisitDto> StartVisitAsync(string id);
    Task<OutboundVisitDto> CompleteVisitAsync(string id, CompleteOutboundVisitRequest request);
    Task<OutboundVisitDto> AddFollowUpAsync(string id, DateOnly followUpDate, int responsibleEmployeeId, string responsibleEmployeeName, string task);
    Task<OutboundVisitDto> CancelVisitAsync(string id, string reason, string cancelledBy);
    Task<OutboundVisitDto> SubmitExistingForApprovalAsync(string id, string actingEmployeeName);
    Task<OutboundVisitDto> AddNoteAsync(string id, string note, string addedBy);
    Task<IReadOnlyList<CrmAccountDto>> GetCrmAccountsAsync();
}
