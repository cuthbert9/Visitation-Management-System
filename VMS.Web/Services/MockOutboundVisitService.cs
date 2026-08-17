using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class MockOutboundVisitService : IOutboundVisitService
{
    private readonly List<OutboundVisitDto> _visits;
    private readonly List<CrmAccountDto> _accounts;
    private int _sequence = 41;

    public MockOutboundVisitService()
    {
        _accounts = BuildAccounts();
        _visits = BuildSeedVisits();
    }

    public Task<IReadOnlyList<OutboundVisitDto>> GetVisitsAsync()
    {
        IReadOnlyList<OutboundVisitDto> result = _visits.OrderByDescending(v => v.UpdatedAt).ToList();
        return Task.FromResult(result);
    }

    public Task<OutboundVisitDto?> GetVisitAsync(string id)
    {
        return Task.FromResult(_visits.FirstOrDefault(v => v.Id == id));
    }

    public Task<IReadOnlyList<CrmAccountDto>> GetCrmAccountsAsync()
    {
        IReadOnlyList<CrmAccountDto> result = _accounts;
        return Task.FromResult(result);
    }

    public Task<OutboundVisitDto> SaveDraftAsync(PlanOutboundVisitRequest request)
    {
        var isNew = string.IsNullOrWhiteSpace(request.ExistingId) || _visits.All(v => v.Id != request.ExistingId);
        var visit = Upsert(request);
        visit.ApprovalStatus = OutboundVisitApprovalStatus.Draft;
        visit.ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled;
        visit.ApprovalStage = null;
        visit.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            visit.ActivityLog.Add(new ApprovalHistoryEntryDto
            {
                Action = ApprovalHistoryAction.Created,
                By = request.ActingEmployeeName,
                Role = visit.Owner.Position ?? "Requester",
                At = visit.UpdatedAt
            });
        }

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> SubmitForApprovalAsync(PlanOutboundVisitRequest request)
    {
        var wasChangesRequested = !string.IsNullOrWhiteSpace(request.ExistingId) &&
            _visits.FirstOrDefault(v => v.Id == request.ExistingId)?.ApprovalStatus == OutboundVisitApprovalStatus.ChangesRequested;

        var visit = Upsert(request);
        var now = DateTime.UtcNow;

        visit.ApprovalStatus = OutboundVisitApprovalStatus.PendingApproval;
        visit.ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled;
        visit.ApprovalStage = DetermineApprovalStage(visit.Category, visit.Priority);
        visit.SubmittedBy = request.ActingEmployeeName;
        visit.SubmittedAt = now;
        visit.UpdatedAt = now;

        var entry = new ApprovalHistoryEntryDto
        {
            Action = wasChangesRequested ? ApprovalHistoryAction.Resubmitted : ApprovalHistoryAction.Submitted,
            By = request.ActingEmployeeName,
            Role = visit.Owner.Position ?? "Requester",
            At = now,
            Comment = wasChangesRequested ? "Resubmitted after requested changes." : null
        };
        visit.ApprovalHistory.Add(entry);
        visit.ActivityLog.Add(entry);

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> SubmitExistingForApprovalAsync(string id, string actingEmployeeName)
    {
        var visit = GetOrThrow(id);
        if (visit.ApprovalStatus is not (OutboundVisitApprovalStatus.Draft or OutboundVisitApprovalStatus.ChangesRequested))
        {
            throw new InvalidOperationException("Only draft or returned requests can be submitted.");
        }

        var wasChangesRequested = visit.ApprovalStatus == OutboundVisitApprovalStatus.ChangesRequested;
        var now = DateTime.UtcNow;

        visit.ApprovalStatus = OutboundVisitApprovalStatus.PendingApproval;
        visit.ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled;
        visit.ApprovalStage = DetermineApprovalStage(visit.Category, visit.Priority);
        visit.SubmittedBy = actingEmployeeName;
        visit.SubmittedAt = now;
        visit.UpdatedAt = now;

        var entry = new ApprovalHistoryEntryDto
        {
            Action = wasChangesRequested ? ApprovalHistoryAction.Resubmitted : ApprovalHistoryAction.Submitted,
            By = actingEmployeeName,
            Role = visit.Owner.Position ?? "Requester",
            At = now
        };
        visit.ApprovalHistory.Add(entry);
        visit.ActivityLog.Add(entry);

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> UpdateDetailsAsync(PlanOutboundVisitRequest request)
    {
        var visit = Upsert(request);
        visit.UpdatedAt = DateTime.UtcNow;
        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.DetailsUpdated,
            By = request.ActingEmployeeName,
            Role = visit.Owner.Position ?? "Requester",
            At = visit.UpdatedAt
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> AddNoteAsync(string id, string note, string addedBy)
    {
        var visit = GetOrThrow(id);
        var now = DateTime.UtcNow;

        visit.AdditionalNotes = string.IsNullOrWhiteSpace(visit.AdditionalNotes)
            ? note
            : $"{visit.AdditionalNotes}\n\n[{now.ToLocalTime():dd MMM yyyy HH:mm}] {note}";
        visit.UpdatedAt = now;

        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.NoteAdded,
            By = addedBy,
            Role = "Team",
            At = now,
            Comment = note
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> DecideApprovalAsync(string id, ApprovalDecisionRequest request)
    {
        var visit = GetOrThrow(id);
        if (visit.ApprovalStatus != OutboundVisitApprovalStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only requests pending approval can be decided.");
        }

        var now = DateTime.UtcNow;

        switch (request.Decision)
        {
            case ApprovalHistoryAction.Approved:
                visit.ApprovalStatus = OutboundVisitApprovalStatus.Approved;
                visit.ExecutionStatus = OutboundVisitExecutionStatus.Scheduled;
                visit.ApprovalStage = "Approved";
                break;
            case ApprovalHistoryAction.Rejected:
                visit.ApprovalStatus = OutboundVisitApprovalStatus.Rejected;
                visit.ExecutionStatus = OutboundVisitExecutionStatus.Cancelled;
                visit.ApprovalStage = "Rejected";
                break;
            case ApprovalHistoryAction.ChangesRequested:
                visit.ApprovalStatus = OutboundVisitApprovalStatus.ChangesRequested;
                visit.ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled;
                visit.ApprovalStage = "Returned for changes";
                break;
            default:
                throw new InvalidOperationException("Unsupported approval decision.");
        }

        visit.UpdatedAt = now;
        var decisionEntry = new ApprovalHistoryEntryDto
        {
            Action = request.Decision,
            By = request.DecidedBy,
            Role = request.DecidedByRole,
            At = now,
            Comment = request.Comment
        };
        visit.ApprovalHistory.Add(decisionEntry);
        visit.ActivityLog.Add(decisionEntry);

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> RescheduleAsync(string id, DateOnly proposedDate, TimeOnly startTime, TimeOnly? endTime)
    {
        var visit = GetOrThrow(id);
        if (visit.ApprovalStatus != OutboundVisitApprovalStatus.Approved || visit.ExecutionStatus != OutboundVisitExecutionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only approved, scheduled visits can be rescheduled.");
        }

        visit.ProposedDate = proposedDate;
        visit.StartTime = startTime;
        visit.EndTime = endTime;
        visit.UpdatedAt = DateTime.UtcNow;
        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.Rescheduled,
            By = visit.Owner.FullName,
            Role = visit.Owner.Position ?? "Visit Owner",
            At = visit.UpdatedAt,
            Comment = $"Rescheduled to {proposedDate:dd MMM yyyy} at {startTime:HH:mm}."
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> StartVisitAsync(string id)
    {
        var visit = GetOrThrow(id);
        if (visit.ApprovalStatus != OutboundVisitApprovalStatus.Approved || visit.ExecutionStatus != OutboundVisitExecutionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only approved, scheduled visits can be started.");
        }

        visit.ExecutionStatus = OutboundVisitExecutionStatus.InProgress;
        visit.UpdatedAt = DateTime.UtcNow;
        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.Started,
            By = visit.Owner.FullName,
            Role = visit.Owner.Position ?? "Visit Owner",
            At = visit.UpdatedAt
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> CompleteVisitAsync(string id, CompleteOutboundVisitRequest request)
    {
        var visit = GetOrThrow(id);
        if (visit.ApprovalStatus != OutboundVisitApprovalStatus.Approved || visit.ExecutionStatus != OutboundVisitExecutionStatus.InProgress)
        {
            throw new InvalidOperationException("Only visits currently in progress can be completed.");
        }

        var now = DateTime.UtcNow;

        visit.ExecutionStatus = OutboundVisitExecutionStatus.Completed;
        visit.Outcome = new OutboundVisitOutcomeDto
        {
            ActualStart = request.ActualStart,
            ActualEnd = request.ActualEnd,
            Result = request.Result,
            DiscussionSummary = request.DiscussionSummary,
            CustomerFeedback = request.CustomerFeedback,
            AgreementsReached = request.AgreementsReached,
            ActionsRequired = request.ActionsRequired,
            Remarks = request.Remarks
        };

        if (request.FollowUpRequired)
        {
            visit.FollowUp = new OutboundVisitFollowUpDto
            {
                FollowUpDate = request.FollowUpDate!.Value,
                ResponsibleEmployeeId = request.FollowUpResponsibleEmployeeId!.Value,
                ResponsibleEmployeeName = request.FollowUpResponsibleEmployeeName ?? string.Empty,
                Task = request.FollowUpTask ?? string.Empty,
                IsCompleted = false
            };
        }
        else
        {
            visit.FollowUp = null;
        }

        visit.UpdatedAt = now;
        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.Completed,
            By = visit.Owner.FullName,
            Role = visit.Owner.Position ?? "Visit Owner",
            At = now,
            Comment = request.DiscussionSummary
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> AddFollowUpAsync(string id, DateOnly followUpDate, int responsibleEmployeeId, string responsibleEmployeeName, string task)
    {
        var visit = GetOrThrow(id);
        if (visit.ExecutionStatus != OutboundVisitExecutionStatus.Completed)
        {
            throw new InvalidOperationException("Follow-ups can only be added to completed visits.");
        }

        var now = DateTime.UtcNow;
        visit.FollowUp = new OutboundVisitFollowUpDto
        {
            FollowUpDate = followUpDate,
            ResponsibleEmployeeId = responsibleEmployeeId,
            ResponsibleEmployeeName = responsibleEmployeeName,
            Task = task,
            IsCompleted = false
        };
        visit.UpdatedAt = now;
        visit.ActivityLog.Add(new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.DetailsUpdated,
            By = responsibleEmployeeName,
            Role = "Follow-up",
            At = now,
            Comment = $"Follow-up created: {task}"
        });

        return Task.FromResult(visit);
    }

    public Task<OutboundVisitDto> CancelVisitAsync(string id, string reason, string cancelledBy)
    {
        var visit = GetOrThrow(id);
        if (visit.ExecutionStatus is OutboundVisitExecutionStatus.Completed or OutboundVisitExecutionStatus.Cancelled)
        {
            throw new InvalidOperationException("Completed or already-cancelled visits cannot be cancelled.");
        }

        var now = DateTime.UtcNow;
        visit.ExecutionStatus = OutboundVisitExecutionStatus.Cancelled;
        visit.UpdatedAt = now;
        var entry = new ApprovalHistoryEntryDto
        {
            Action = ApprovalHistoryAction.Cancelled,
            By = cancelledBy,
            Role = "Requester",
            At = now,
            Comment = reason
        };
        visit.ApprovalHistory.Add(entry);
        visit.ActivityLog.Add(entry);

        return Task.FromResult(visit);
    }

    private OutboundVisitDto GetOrThrow(string id)
    {
        return _visits.FirstOrDefault(v => v.Id == id)
            ?? throw new InvalidOperationException($"Outbound visit '{id}' was not found.");
    }

    private OutboundVisitDto Upsert(PlanOutboundVisitRequest request)
    {
        var now = DateTime.UtcNow;
        var existing = string.IsNullOrWhiteSpace(request.ExistingId)
            ? null
            : _visits.FirstOrDefault(v => v.Id == request.ExistingId);

        var owner = request.Owner;
        var team = request.TeamMembers
            .Where(member => owner is null || member.Id != owner.Id)
            .ToList();

        var visit = existing ?? new OutboundVisitDto
        {
            Id = GenerateId(),
            RequestNo = GenerateRequestNo(),
            CreatedAt = now
        };

        visit.Title = request.Title;
        visit.Purpose = request.Purpose;
        visit.Priority = request.Priority;
        visit.Category = request.Category;
        visit.Agenda = request.Agenda;
        visit.ProposedDate = request.ProposedDate;
        visit.StartTime = request.StartTime;
        visit.EndTime = request.EndTime;
        visit.StakeholderType = request.StakeholderType;
        visit.Organization = request.Organization;
        visit.ContactPerson = request.ContactPerson;
        visit.ContactNumber = request.ContactNumber;
        visit.MeetingLocation = request.MeetingLocation;
        visit.Address = request.Address;
        visit.Owner = owner ?? visit.Owner ?? UnassignedEmployee;
        visit.TeamMembers = team;
        visit.DepartmentId = visit.Owner.DepartmentId;
        visit.DepartmentName = visit.Owner.DepartmentName;
        visit.BusinessObjective = request.BusinessObjective;
        visit.ExpectedOutcome = request.ExpectedOutcome;
        visit.Justification = request.Justification;
        visit.AdditionalNotes = request.AdditionalNotes;
        visit.Documents = request.DocumentFileNames
            .Select((fileName, index) => new OutboundVisitDocumentDto
            {
                Id = index + 1,
                FileName = fileName,
                SizeBytes = Random.Shared.Next(48_000, 2_400_000),
                UploadedAt = now
            })
            .ToList();

        if (existing is null)
        {
            _visits.Add(visit);
        }

        return visit;
    }

    private static string DetermineApprovalStage(OutboundVisitCategory category, OutboundVisitPriority priority)
    {
        if (category == OutboundVisitCategory.SpecialEngagement || priority == OutboundVisitPriority.Critical)
        {
            return "Pending Director Approval";
        }

        if (category == OutboundVisitCategory.Urgent || priority == OutboundVisitPriority.High)
        {
            return "Pending Department Manager Approval";
        }

        return "Pending Sales Manager Approval";
    }

    private string GenerateId()
    {
        _sequence++;
        return $"OUT-{DateTime.UtcNow.Year}-{_sequence:D4}";
    }

    private string GenerateRequestNo()
    {
        return $"REQ-{_sequence:D4}";
    }

    private static readonly EmployeeDto UnassignedEmployee = new()
    {
        Id = 0,
        EmployeeNumber = string.Empty,
        FullName = "Unassigned",
        Position = null,
        DepartmentId = 0,
        DepartmentName = "Unassigned",
        IsActive = true
    };

    private static readonly List<EmployeeDto> Employees =
    [
        new() { Id = 501, EmployeeNumber = "EMP-1001", FullName = "Daniel Mwenda", Position = "Sales Representative", DepartmentId = 1, DepartmentName = "Sales & Marketing", IsActive = true, Email = "daniel.mwenda@msd.co.ke" },
        new() { Id = 502, EmployeeNumber = "EMP-1002", FullName = "Njeri Kamau", Position = "Technical Expert", DepartmentId = 2, DepartmentName = "Technical Services", IsActive = true, Email = "njeri.kamau@msd.co.ke" },
        new() { Id = 503, EmployeeNumber = "EMP-1003", FullName = "Victor Odinga", Position = "Executive", DepartmentId = 3, DepartmentName = "Executive Office", IsActive = true, Email = "victor.odinga@msd.co.ke" },
        new() { Id = 504, EmployeeNumber = "EMP-1004", FullName = "Christine Wambui", Position = "Support Representative", DepartmentId = 4, DepartmentName = "Customer Support", IsActive = true, Email = "christine.wambui@msd.co.ke" },
        new() { Id = 505, EmployeeNumber = "EMP-1005", FullName = "Peter Mburu", Position = "Team Lead", DepartmentId = 1, DepartmentName = "Sales & Marketing", IsActive = true, Email = "peter.mburu@msd.co.ke" },
        new() { Id = 506, EmployeeNumber = "EMP-1006", FullName = "Aisha Noor", Position = "Sales Representative", DepartmentId = 1, DepartmentName = "Sales & Marketing", IsActive = true, Email = "aisha.noor@msd.co.ke" },
        new() { Id = 507, EmployeeNumber = "EMP-1007", FullName = "Kevin Otieno", Position = "Technical Expert", DepartmentId = 2, DepartmentName = "Technical Services", IsActive = true, Email = "kevin.otieno@msd.co.ke" }
    ];

    private static List<CrmAccountDto> BuildAccounts() =>
    [
        new() { Id = 1, Organization = "ABC Corporation", Type = StakeholderType.CorporateClient, ContactPerson = "Grace Mwangi", ContactTitle = "Procurement Manager", ContactNumber = "+254 712 400 111", Address = "Kenyatta Avenue, Nairobi" },
        new() { Id = 2, Organization = "Meridian Retail Group", Type = StakeholderType.Customer, ContactPerson = "Peter Wanjiru", ContactTitle = "Store Operations Lead", ContactNumber = "+254 722 400 222", Address = "Mombasa Road, Nairobi" },
        new() { Id = 3, Organization = "Highline Logistics", Type = StakeholderType.Partner, ContactPerson = "Amina Yusuf", ContactTitle = "Partnerships Director", ContactNumber = "+254 733 400 333", Address = "Airport North Road, Nairobi" },
        new() { Id = 4, Organization = "Baraka Foods Ltd", Type = StakeholderType.Customer, ContactPerson = "Samuel Kiptoo", ContactTitle = "Finance Manager", ContactNumber = "+254 744 400 444", Address = "Thika Road, Ruiru" },
        new() { Id = 5, Organization = "Coastal Beverages Co.", Type = StakeholderType.Prospect, ContactPerson = "Linda Achieng", ContactTitle = "Business Development", ContactNumber = "+254 755 400 555", Address = "Nyali, Mombasa" },
        new() { Id = 6, Organization = "Nova Textiles", Type = StakeholderType.Customer, ContactPerson = "Brian Odhiambo", ContactTitle = "Plant Manager", ContactNumber = "+254 766 400 666", Address = "Kisumu Industrial Area" },
        new() { Id = 7, Organization = "Equator Freight", Type = StakeholderType.Supplier, ContactPerson = "Faith Nyambura", ContactTitle = "Operations Manager", ContactNumber = "+254 777 400 777", Address = "Nakuru Highway" }
    ];

    private List<OutboundVisitDto> BuildSeedVisits()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        List<OutboundVisitDto> visits =
        [
            new()
            {
                Id = "OUT-2026-0035",
                RequestNo = "REQ-0035",
                Title = "Product Demonstration Prep — Meridian Retail Group",
                Purpose = OutboundVisitPurpose.NewBusiness,
                Category = OutboundVisitCategory.Routine,
                Priority = OutboundVisitPriority.Medium,
                Agenda = "Walk through the new POS integration and pricing tiers.",
                ProposedDate = today.AddDays(9),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 30),
                StakeholderType = StakeholderType.Customer,
                Organization = "Meridian Retail Group",
                ContactPerson = "Peter Wanjiru",
                ContactNumber = "+254 722 400 222",
                MeetingLocation = "Meridian HQ, Mombasa Road",
                Address = "Mombasa Road, Nairobi",
                Owner = Employees[0],
                TeamMembers = [Employees[1]],
                DepartmentId = Employees[0].DepartmentId,
                DepartmentName = Employees[0].DepartmentName,
                BusinessObjective = "Introduce the new POS integration ahead of Q4 rollout.",
                ExpectedOutcome = "Verbal agreement to a pilot in two stores.",
                Justification = "Meridian is our second-largest retail account and has requested this demo directly.",
                AdditionalNotes = "Bring the tablet demo unit.",
                ApprovalStatus = OutboundVisitApprovalStatus.Draft,
                ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = "OUT-2026-0036",
                RequestNo = "REQ-0036",
                Title = "Partnership Discussion — Highline Logistics",
                Purpose = OutboundVisitPurpose.PartnershipDevelopment,
                Category = OutboundVisitCategory.Urgent,
                Priority = OutboundVisitPriority.High,
                Agenda = "Discuss a joint distribution agreement for the coastal region.",
                ProposedDate = today.AddDays(4),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 30),
                StakeholderType = StakeholderType.Partner,
                Organization = "Highline Logistics",
                ContactPerson = "Amina Yusuf",
                ContactNumber = "+254 733 400 333",
                MeetingLocation = "Highline Logistics offices",
                Address = "Airport North Road, Nairobi",
                Owner = Employees[4],
                TeamMembers = [Employees[2]],
                DepartmentId = Employees[4].DepartmentId,
                DepartmentName = Employees[4].DepartmentName,
                BusinessObjective = "Secure a joint distribution agreement for the coastal region.",
                ExpectedOutcome = "Signed letter of intent.",
                Justification = "Highline approached us first; a competitor is reportedly in parallel talks.",
                SubmittedBy = "Peter Mburu",
                SubmittedAt = now.AddDays(-1),
                ApprovalStatus = OutboundVisitApprovalStatus.PendingApproval,
                ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled,
                ApprovalStage = "Pending Department Manager Approval",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Peter Mburu", Role = "Team Lead", At = now.AddDays(-1) }
                ],
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = "OUT-2026-0037",
                RequestNo = "REQ-0037",
                Title = "Contract Renewal Visit — Baraka Foods Ltd",
                Purpose = OutboundVisitPurpose.ContractNegotiation,
                Category = OutboundVisitCategory.Routine,
                Priority = OutboundVisitPriority.Medium,
                Agenda = "Present the renewal terms for the annual supply contract.",
                ProposedDate = today.AddDays(6),
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0),
                StakeholderType = StakeholderType.Customer,
                Organization = "Baraka Foods Ltd",
                ContactPerson = "Samuel Kiptoo",
                ContactNumber = "+254 744 400 444",
                MeetingLocation = "Baraka Foods offices",
                Address = "Thika Road, Ruiru",
                Owner = Employees[0],
                TeamMembers = [],
                DepartmentId = Employees[0].DepartmentId,
                DepartmentName = Employees[0].DepartmentName,
                BusinessObjective = "Renew the annual supply contract before it lapses next month.",
                ExpectedOutcome = "Signed renewal.",
                Justification = "Contract expires in 5 weeks; renewal terms need sign-off before this visit can be rescheduled.",
                SubmittedBy = "Daniel Mwenda",
                SubmittedAt = now.AddDays(-4),
                ApprovalStatus = OutboundVisitApprovalStatus.ChangesRequested,
                ExecutionStatus = OutboundVisitExecutionStatus.NotScheduled,
                ApprovalStage = "Returned for changes",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Daniel Mwenda", Role = "Sales Representative", At = now.AddDays(-4) },
                    new() { Action = ApprovalHistoryAction.ChangesRequested, By = "James Kariuki", Role = "Sales Manager", At = now.AddDays(-3), Comment = "Please attach the updated pricing schedule before resubmitting." }
                ],
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-3)
            },
            new()
            {
                Id = "OUT-2026-0038",
                RequestNo = "REQ-0038",
                Title = "Site Survey — Coastal Beverages Co.",
                Purpose = OutboundVisitPurpose.SiteSurvey,
                Category = OutboundVisitCategory.Routine,
                Priority = OutboundVisitPriority.Low,
                Agenda = "Survey the bottling site for a potential distribution partnership.",
                ProposedDate = today.AddDays(-2),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(12, 0),
                StakeholderType = StakeholderType.Prospect,
                Organization = "Coastal Beverages Co.",
                ContactPerson = "Linda Achieng",
                ContactNumber = "+254 755 400 555",
                MeetingLocation = "Coastal Beverages bottling plant",
                Address = "Nyali, Mombasa",
                Owner = Employees[5],
                TeamMembers = [],
                DepartmentId = Employees[5].DepartmentId,
                DepartmentName = Employees[5].DepartmentName,
                BusinessObjective = "Assess site suitability ahead of a distribution proposal.",
                Justification = "Prospect has low near-term volume; travel cost doesn't justify a survey visit this quarter.",
                SubmittedBy = "Aisha Noor",
                SubmittedAt = now.AddDays(-6),
                ApprovalStatus = OutboundVisitApprovalStatus.Rejected,
                ExecutionStatus = OutboundVisitExecutionStatus.Cancelled,
                ApprovalStage = "Rejected",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Aisha Noor", Role = "Sales Representative", At = now.AddDays(-6) },
                    new() { Action = ApprovalHistoryAction.Rejected, By = "James Kariuki", Role = "Sales Manager", At = now.AddDays(-5), Comment = "Hold until Coastal Beverages confirms minimum order volume — revisit next quarter." }
                ],
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = "OUT-2026-0039",
                RequestNo = "REQ-0039",
                Title = "Account Review — Nova Textiles",
                Purpose = OutboundVisitPurpose.AccountReview,
                Category = OutboundVisitCategory.Routine,
                Priority = OutboundVisitPriority.Medium,
                Agenda = "Quarterly account review and order forecast for Q1.",
                ProposedDate = today.AddDays(3),
                StartTime = new TimeOnly(9, 30),
                EndTime = new TimeOnly(10, 30),
                StakeholderType = StakeholderType.Customer,
                Organization = "Nova Textiles",
                ContactPerson = "Brian Odhiambo",
                ContactNumber = "+254 766 400 666",
                MeetingLocation = "Nova Textiles plant office",
                Address = "Kisumu Industrial Area",
                Owner = Employees[0],
                TeamMembers = [Employees[3]],
                DepartmentId = Employees[0].DepartmentId,
                DepartmentName = Employees[0].DepartmentName,
                BusinessObjective = "Review account performance and confirm Q1 order forecast.",
                ExpectedOutcome = "Confirmed forecast and any open support items resolved.",
                Justification = "Standard quarterly review for a top-10 account.",
                SubmittedBy = "Daniel Mwenda",
                SubmittedAt = now.AddDays(-3),
                ApprovalStatus = OutboundVisitApprovalStatus.Approved,
                ExecutionStatus = OutboundVisitExecutionStatus.Scheduled,
                ApprovalStage = "Approved",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Daniel Mwenda", Role = "Sales Representative", At = now.AddDays(-3) },
                    new() { Action = ApprovalHistoryAction.Approved, By = "James Kariuki", Role = "Sales Manager", At = now.AddDays(-2), Comment = "Approved — good standing account." }
                ],
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = "OUT-2026-0040",
                RequestNo = "REQ-0040",
                Title = "Technical Support Visit — Equator Freight",
                Purpose = OutboundVisitPurpose.TechnicalSupport,
                Category = OutboundVisitCategory.Urgent,
                Priority = OutboundVisitPriority.High,
                Agenda = "Diagnose recurring integration errors on the freight tracking API.",
                ProposedDate = today.AddDays(-5),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(15, 0),
                StakeholderType = StakeholderType.Supplier,
                Organization = "Equator Freight",
                ContactPerson = "Faith Nyambura",
                ContactNumber = "+254 777 400 777",
                MeetingLocation = "Equator Freight depot office",
                Address = "Nakuru Highway",
                Owner = Employees[1],
                TeamMembers = [Employees[6]],
                DepartmentId = Employees[1].DepartmentId,
                DepartmentName = Employees[1].DepartmentName,
                BusinessObjective = "Resolve the recurring API integration errors reported by Equator Freight.",
                ExpectedOutcome = "Root cause identified and a fix scheduled.",
                Justification = "Equator has escalated twice this month; SLA is at risk.",
                SubmittedBy = "Njeri Kamau",
                SubmittedAt = now.AddDays(-8),
                ApprovalStatus = OutboundVisitApprovalStatus.Approved,
                ExecutionStatus = OutboundVisitExecutionStatus.Completed,
                ApprovalStage = "Approved",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Njeri Kamau", Role = "Technical Expert", At = now.AddDays(-8) },
                    new() { Action = ApprovalHistoryAction.Approved, By = "Victor Odinga", Role = "Executive", At = now.AddDays(-7), Comment = "Approved — SLA risk, proceed immediately." }
                ],
                Outcome = new OutboundVisitOutcomeDto
                {
                    ActualStart = now.AddDays(-5).Date.AddHours(13),
                    ActualEnd = now.AddDays(-5).Date.AddHours(15).AddMinutes(20),
                    Result = OutboundVisitOutcomeResult.Successful,
                    DiscussionSummary = "Traced the errors to a stale API key on Equator's gateway; rotated the key and validated three consecutive successful syncs on site.",
                    CustomerFeedback = "Relieved the fix was immediate; asked for a written summary for their IT team.",
                    AgreementsReached = "Equator will rotate the key on their side every 90 days going forward.",
                    ActionsRequired = "Send written incident summary and rotation schedule.",
                    Remarks = "No further on-site visits expected for this issue."
                },
                FollowUp = new OutboundVisitFollowUpDto
                {
                    FollowUpDate = today.AddDays(2),
                    ResponsibleEmployeeId = Employees[1].Id,
                    ResponsibleEmployeeName = Employees[1].FullName,
                    Task = "Send written incident summary and key-rotation schedule to Equator Freight's IT team.",
                    IsCompleted = false
                },
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = "OUT-2026-0041",
                RequestNo = "REQ-0041",
                Title = "Customer Relationship Visit — ABC Corporation",
                Purpose = OutboundVisitPurpose.CustomerRelationshipVisit,
                Category = OutboundVisitCategory.SpecialEngagement,
                Priority = OutboundVisitPriority.High,
                Agenda = "Annual relationship visit with the ABC Corporation executive team.",
                ProposedDate = today,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 30),
                StakeholderType = StakeholderType.CorporateClient,
                Organization = "ABC Corporation",
                ContactPerson = "Grace Mwangi",
                ContactNumber = "+254 712 400 111",
                MeetingLocation = "ABC Corporation head office",
                Address = "Kenyatta Avenue, Nairobi",
                Owner = Employees[4],
                TeamMembers = [Employees[2], Employees[0]],
                DepartmentId = Employees[4].DepartmentId,
                DepartmentName = Employees[4].DepartmentName,
                BusinessObjective = "Strengthen the executive relationship ahead of the annual contract renewal.",
                ExpectedOutcome = "Verbal commitment to renew at current volumes.",
                Justification = "ABC is our largest corporate account; the CEO specifically requested this visit.",
                SubmittedBy = "Peter Mburu",
                SubmittedAt = now.AddDays(-10),
                ApprovalStatus = OutboundVisitApprovalStatus.Approved,
                ExecutionStatus = OutboundVisitExecutionStatus.InProgress,
                ApprovalStage = "Approved",
                ApprovalHistory =
                [
                    new() { Action = ApprovalHistoryAction.Submitted, By = "Peter Mburu", Role = "Team Lead", At = now.AddDays(-10) },
                    new() { Action = ApprovalHistoryAction.Approved, By = "Margaret Achieng", Role = "Director", At = now.AddDays(-9), Comment = "Approved — please debrief the executive team immediately after." }
                ],
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddHours(-1)
            }
        ];

        return visits;
    }
}
