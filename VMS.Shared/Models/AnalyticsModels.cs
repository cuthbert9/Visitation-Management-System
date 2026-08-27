namespace VisitorManagementSystem.Shared.Models;

public class VisitsDashboardDto
{
    public int TotalVisitsToday { get; set; }
    public int AwaitingHost { get; set; }
    public int OnSiteNow { get; set; }
    public int Overdue { get; set; }
    public int ClosedToday { get; set; }
}
