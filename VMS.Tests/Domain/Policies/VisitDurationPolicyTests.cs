using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Domain.Policies;

namespace VMS.Tests.Domain.Policies;

public class VisitDurationPolicyTests
{
    [Theory]
    [InlineData(VisitPurposeType.Official, "DirectorGeneral", 4)]
    [InlineData(VisitPurposeType.Official, "Director", 1)]
    [InlineData(VisitPurposeType.Official, "Manager", 1)]
    [InlineData(VisitPurposeType.OfficialMeeting, "Officer", 2)]
    [InlineData(VisitPurposeType.Technician, "AnyPosition", 8)]
    [InlineData(VisitPurposeType.Facilitator, null, 3)]
    [InlineData(VisitPurposeType.ExternalAuditor, null, 10)]
    public void TryGetProposedDuration_KnownPurposeAndPosition_ReturnsExpectedDuration(
        VisitPurposeType purpose, string? hostPosition, int expectedHours)
    {
        var duration = VisitDurationPolicy.TryGetProposedDuration(purpose, hostPosition);

        Assert.Equal(TimeSpan.FromHours(expectedHours), duration);
    }

    [Fact]
    public void TryGetProposedDuration_OfficialWithUnhandledPosition_ReturnsNull()
    {
        var duration = VisitDurationPolicy.TryGetProposedDuration(VisitPurposeType.Official, "Intern");

        Assert.Null(duration);
    }

    [Theory]
    [InlineData(VisitPurposeType.Personal)]
    [InlineData(VisitPurposeType.Delivery)]
    [InlineData(VisitPurposeType.Other)]
    public void TryGetProposedDuration_PurposeWithNoPolicy_ReturnsNull(VisitPurposeType purpose)
    {
        var duration = VisitDurationPolicy.TryGetProposedDuration(purpose, "Manager");

        Assert.Null(duration);
    }
}
