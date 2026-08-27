namespace VisitorManagementSystem.Shared.Models;

public enum OutboundVisitApprovalStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    ChangesRequested = 4,
    Rejected = 5
}

public enum OutboundVisitExecutionStatus
{
    NotScheduled = 1,
    Scheduled = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum OutboundVisitPurpose
{
    CustomerRelationshipVisit = 1,
    NewBusiness = 2,
    AccountReview = 3,
    TechnicalSupport = 4,
    PartnershipDevelopment = 5,
    AuditInspection = 6,
    SiteSurvey = 7,
    ContractNegotiation = 8
}

public enum OutboundVisitCategory
{
    Routine = 1,
    Urgent = 2,
    SpecialEngagement = 3
}

public enum OutboundVisitPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum StakeholderType
{
    Customer = 1,
    Prospect = 2,
    CorporateClient = 3,
    Partner = 4,
    Supplier = 5,
    Stakeholder = 6,
    Other = 7
}

public enum OutboundVisitOutcomeResult
{
    Successful = 1,
    PartiallySuccessful = 2,
    Unsuccessful = 3,
    PostponedByCustomer = 4
}

public enum ApprovalHistoryAction
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    ChangesRequested = 4,
    Resubmitted = 5,
    Cancelled = 6,
    Started = 7,
    Completed = 8,
    Rescheduled = 9,
    NoteAdded = 10,
    DetailsUpdated = 11,
    Created = 12
}

public class ApprovalHistoryEntryDto
{
    public ApprovalHistoryAction Action { get; set; }
    public string By { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime At { get; set; }
    public string? Comment { get; set; }
}

public class OutboundVisitDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class OutboundVisitOutcomeDto
{
    public DateTime ActualStart { get; set; }
    public DateTime ActualEnd { get; set; }
    public OutboundVisitOutcomeResult Result { get; set; }
    public string DiscussionSummary { get; set; } = string.Empty;
    public string? CustomerFeedback { get; set; }
    public string? AgreementsReached { get; set; }
    public string? ActionsRequired { get; set; }
    public string? Remarks { get; set; }
}

public class OutboundVisitFollowUpDto
{
    public DateOnly FollowUpDate { get; set; }
    public int ResponsibleEmployeeId { get; set; }
    public string ResponsibleEmployeeName { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class CrmAccountDto
{
    public int Id { get; set; }
    public string Organization { get; set; } = string.Empty;
    public StakeholderType Type { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class OutboundVisitDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public OutboundVisitPurpose Purpose { get; set; }
    public OutboundVisitCategory Category { get; set; }
    public OutboundVisitPriority Priority { get; set; }
    public string? Agenda { get; set; }

    public DateOnly ProposedDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public StakeholderType StakeholderType { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactNumber { get; set; }
    public string? MeetingLocation { get; set; }
    public string? Address { get; set; }

    public EmployeeDto Owner { get; set; } = null!;
    public List<EmployeeDto> TeamMembers { get; set; } = [];
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public string? BusinessObjective { get; set; }
    public string? ExpectedOutcome { get; set; }
    public string? Justification { get; set; }
    public string? AdditionalNotes { get; set; }

    public List<OutboundVisitDocumentDto> Documents { get; set; } = [];

    public string SubmittedBy { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }

    public OutboundVisitApprovalStatus ApprovalStatus { get; set; }
    public OutboundVisitExecutionStatus ExecutionStatus { get; set; }
    public string? ApprovalStage { get; set; }

    public List<ApprovalHistoryEntryDto> ApprovalHistory { get; set; } = [];
    public List<ApprovalHistoryEntryDto> ActivityLog { get; set; } = [];

    public OutboundVisitOutcomeDto? Outcome { get; set; }
    public OutboundVisitFollowUpDto? FollowUp { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PlanOutboundVisitRequest
{
    public string? ExistingId { get; set; }

    public string Title { get; set; } = string.Empty;
    public OutboundVisitPurpose Purpose { get; set; }
    public OutboundVisitPriority Priority { get; set; } = OutboundVisitPriority.Medium;
    public OutboundVisitCategory Category { get; set; } = OutboundVisitCategory.Routine;
    public string? Agenda { get; set; }

    public DateOnly ProposedDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public int? CrmAccountId { get; set; }
    public StakeholderType StakeholderType { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactNumber { get; set; }
    public string? MeetingLocation { get; set; }
    public string? Address { get; set; }

    public EmployeeDto? Owner { get; set; }
    public List<EmployeeDto> TeamMembers { get; set; } = [];

    public string BusinessObjective { get; set; } = string.Empty;
    public string? ExpectedOutcome { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }

    public List<string> DocumentFileNames { get; set; } = [];

    public string ActingEmployeeName { get; set; } = string.Empty;
}

public class ApprovalDecisionRequest
{
    public ApprovalHistoryAction Decision { get; set; }
    public string? Comment { get; set; }
    public string DecidedBy { get; set; } = string.Empty;
    public string DecidedByRole { get; set; } = string.Empty;
}

public class CompleteOutboundVisitRequest
{
    public DateTime ActualStart { get; set; }
    public DateTime ActualEnd { get; set; }
    public OutboundVisitOutcomeResult Result { get; set; }
    public string DiscussionSummary { get; set; } = string.Empty;
    public string? CustomerFeedback { get; set; }
    public string? AgreementsReached { get; set; }
    public string? ActionsRequired { get; set; }
    public string? Remarks { get; set; }

    public bool FollowUpRequired { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public int? FollowUpResponsibleEmployeeId { get; set; }
    public string? FollowUpResponsibleEmployeeName { get; set; }
    public string? FollowUpTask { get; set; }
}
