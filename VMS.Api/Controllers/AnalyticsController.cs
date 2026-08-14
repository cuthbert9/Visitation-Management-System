using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private static readonly VisitStatus[] AwaitingHostStatuses =
    [
        VisitStatus.Registered,
        VisitStatus.WaitingForHost,
        VisitStatus.HostAcknowledged
    ];

    private static readonly VisitStatus[] OnSiteStatuses =
    [
        VisitStatus.Attended,
        VisitStatus.AwaitingExit
    ];

    private static readonly VisitStatus[] TerminalStatuses =
    [
        VisitStatus.Completed,
        VisitStatus.Closed,
        VisitStatus.Cancelled,
        VisitStatus.Denied
    ];

    private readonly AppDbContext _context;

    public AnalyticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("visits-dashboard")]
    public async Task<ActionResult<VisitsDashboardDto>> GetVisitsDashboard()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        var totalVisitsToday = await _context.Visits
            .CountAsync(visit => visit.ArrivalTime >= todayStart && visit.ArrivalTime < todayEnd);

        var awaitingHost = await _context.Visits
            .CountAsync(visit => AwaitingHostStatuses.Contains(visit.Status));

        var onSiteNow = await _context.Visits
            .CountAsync(visit => OnSiteStatuses.Contains(visit.Status));

        var overdue = await _context.Visits
            .CountAsync(visit =>
                !TerminalStatuses.Contains(visit.Status) &&
                visit.ExpectedDepartureTime != null &&
                visit.ExpectedDepartureTime < now);

        var closedToday = await _context.Visits
            .CountAsync(visit =>
                visit.Status == VisitStatus.Closed &&
                visit.ClosedAt != null &&
                visit.ClosedAt >= todayStart && visit.ClosedAt < todayEnd);

        return Ok(new VisitsDashboardDto
        {
            TotalVisitsToday = totalVisitsToday,
            AwaitingHost = awaitingHost,
            OnSiteNow = onSiteNow,
            Overdue = overdue,
            ClosedToday = closedToday
        });
    }
}
