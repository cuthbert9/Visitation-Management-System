using VisitorManagementSystem.Domain.Enums;

namespace VisitorManagementSystem.Domain.Policies;

public static class VisitDurationPolicy
{
    public static TimeSpan? TryGetProposedDuration(VisitPurposeType purpose, string? hostPosition) => purpose switch
    {
        VisitPurposeType.Official when hostPosition == "DirectorGeneral" => TimeSpan.FromHours(4),
        VisitPurposeType.Official when hostPosition == "Director" => TimeSpan.FromHours(1),
        VisitPurposeType.Official when hostPosition == "Manager" => TimeSpan.FromHours(1),
        VisitPurposeType.Official when hostPosition == "Officer" => TimeSpan.FromMinutes(30),
        VisitPurposeType.OfficialMeeting => TimeSpan.FromHours(2),
        VisitPurposeType.Technician => TimeSpan.FromHours(8),
        VisitPurposeType.Facilitator => TimeSpan.FromHours(3),
        VisitPurposeType.ExternalAuditor => TimeSpan.FromHours(10),
        _ => null
    };
}

