using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Domain.Policies;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private static readonly VisitStatus[] CancellableStatuses =
    [
        VisitStatus.Registered,
        VisitStatus.WaitingForHost,
        VisitStatus.HostAcknowledged,
        VisitStatus.Attended
    ];

    private static readonly VisitStatus[] ClosedStatuses =
    [
        VisitStatus.Completed,
        VisitStatus.Closed,
        VisitStatus.Cancelled,
        VisitStatus.Denied
    ];

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailService _emailService;
    private readonly ILogger<VisitsController> _logger;

    public VisitsController(
        AppDbContext context,
        IAuditLogService auditLog,
        IEmailService emailService,
        ILogger<VisitsController> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<VisitDto>> Create([FromBody] VisitCreateDto request)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(visitor => visitor.Id == request.VisitorId);
        if (visitor is null)
        {
            return BadRequest(new { message = "Visitor does not exist." });
        }

        Employee? hostEmployee = null;
        if (request.HostEmployeeId.HasValue)
        {
            hostEmployee = await _context.Employees.FirstOrDefaultAsync(employee => employee.Id == request.HostEmployeeId.Value);
            if (hostEmployee is null)
            {
                return BadRequest(new { message = "Host employee does not exist." });
            }
        }

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await _context.Departments.AnyAsync(department => department.Id == request.DepartmentId.Value);
            if (!departmentExists)
            {
                return BadRequest(new { message = "Department does not exist." });
            }
        }

        var checkedInByExists = await _context.Users.AnyAsync(user => user.Id == request.CheckedInById);

        if (!checkedInByExists)
        {
            return BadRequest(new { message = "Checking-in user does not exist." });
        }

        var now = DateTime.UtcNow;
        var hasBadge = !string.IsNullOrWhiteSpace(request.BadgeNumber);
        var isFullyRegistered = request.HostEmployeeId.HasValue && request.DepartmentId.HasValue && request.Purpose.HasValue;
        var proposedDuration = hostEmployee is not null
            ? VisitDurationPolicy.TryGetProposedDuration(request.Purpose!.Value, hostEmployee.Position)
            : null;

        var visit = new Visit
        {
            VisitNumber = await GenerateVisitNumberAsync(now),
            VisitorId = request.VisitorId,
            HostEmployeeId = request.HostEmployeeId,
            DepartmentId = request.DepartmentId,
            Purpose = request.Purpose,
            PurposeDescription = request.PurposeDescription,
            Status = isFullyRegistered ? VisitStatus.Registered : VisitStatus.GateRegistered,
            ArrivalTime = now,
            ExpectedDepartureTime = request.ExpectedDepartureTime ?? (proposedDuration.HasValue ? now.Add(proposedDuration.Value) : null),
            VehicleModel = request.VehicleModel,
            VehiclePlateNumber = request.VehiclePlateNumber,
            BadgeNumber = request.BadgeNumber,
            BadgeStatus = hasBadge ? BadgeStatus.Issued : null,
            BadgeIssuedAt = hasBadge ? now : null,
            CheckedInById = request.CheckedInById,
            AttachmentUrl = request.AttachmentUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        _context.VisitStatusHistories.Add(new VisitStatusHistory
        {
            VisitId = visit.Id,
            FromStatus = null,
            ToStatus = visit.Status,
            ChangedByUserId = request.CheckedInById,
            ChangedAt = now
        });
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(AuditAction.CheckIn, nameof(Visit), visit.Id, request.CheckedInById, "Visitor registered and checked in.");
        if (hasBadge)
        {
            await _auditLog.LogAsync(AuditAction.BadgeAssigned, nameof(Visit), visit.Id, request.CheckedInById, $"Badge {visit.BadgeNumber} issued.");
        }

        if (isFullyRegistered && !string.IsNullOrWhiteSpace(visitor.Email))
        {
            _logger.LogInformation("Registration email requested for visit {VisitId} to {Email}.", visit.Id, visitor.Email);

            try
            {
                var eastAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
                var arrivalDisplay = TimeZoneInfo.ConvertTimeFromUtc(visit.ArrivalTime, eastAfricaTimeZone);
                var expectedDepartureDisplay = visit.ExpectedDepartureTime.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(visit.ExpectedDepartureTime.Value, eastAfricaTimeZone).ToString("yyyy-MM-dd HH:mm:ss")
                    : "Not specified";

                var subject = "Visit registration successful";

                var body = $"""
                    <div style="margin:0;padding:32px 16px;background-color:#f4f7fb;font-family:Arial,Helvetica,sans-serif;">
                        <div style="max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden;box-shadow:0 6px 18px rgba(15,76,129,0.08);">
                            <div style="padding:28px 24px 18px 24px;text-align:center;background:linear-gradient(180deg,#ffffff 0%,#f8fbff 100%);border-bottom:1px solid #eef2f7;">
                                <img src="https://zdqixwcixanigsaucwek.supabase.co/storage/v1/object/public/Attachments/magerp-logo.svg" alt="MagERP Logo" style="max-width:160px;width:100%;height:auto;display:block;margin:0 auto 16px auto;" />
                                <div style="font-size:12px;letter-spacing:1.2px;text-transform:uppercase;color:#0f4c81;font-weight:700;">MagERP VMS</div>
                                <h2 style="margin:10px 0 0 0;font-size:24px;line-height:1.3;color:#111827;">Visit Registration Successful</h2>
                            </div>

                            <div style="padding:28px 24px;color:#1f2937;line-height:1.7;">
                                <p style="margin:0 0 14px 0;font-size:16px;color:#111827;">
                                    <strong>{visitor.FullName}</strong>, Thank you for using MagERP.
                                </p>

                                <p style="margin:0 0 22px 0;font-size:15px;color:#4b5563;">
                                    Your visit registration was completed successfully. Please find your registration details below.
                                </p>

                                <div style="background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:12px;padding:18px 16px;margin-bottom:22px;">
                                    <p style="margin:0 0 12px 0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Registration ID:</strong> {visit.VisitNumber}
                                    </p>
                                    <p style="margin:0 0 12px 0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Registered At :</strong> {arrivalDisplay:yyyy-MM-dd HH:mm:ss}
                                    </p>
                                    <p style="margin:0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Expected Departure :</strong> {expectedDepartureDisplay}
                                    </p>
                                </div>

                                <p style="margin:0;font-size:14px;color:#6b7280;">
                                    Please keep this email for your records. If you need any assistance, kindly contact the administrator.
                                </p>
                            </div>

                            <div style="padding:16px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;text-align:center;">
                                <p style="margin:0;font-size:13px;color:#6b7280;">Powered by <strong style="color:#0f4c81;">MagERP VMS</strong></p>
                            </div>
                        </div>
                    </div>
                """;

                await _emailService.SendEmailAsync(visitor.Email, subject, body);

                _logger.LogInformation("Registration email successfully sent for visit {VisitId} to {Email}.", visit.Id, visitor.Email);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send registration email for visit {VisitId} to {Email}.", visit.Id, visitor.Email);
            }
        }
        else
        {
            _logger.LogInformation(
                "Registration email skipped for visit {VisitId}: fully registered = {IsFullyRegistered}, has email = {HasEmail}.",
                visit.Id, isFullyRegistered, !string.IsNullOrWhiteSpace(visitor.Email));
        }

        var createdVisit = await GetVisitEntity(visit.Id);
        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, VisitMappings.ToVisitDto(createdVisit!));
    }

    [HttpPatch("{id:int}/complete-handover")]
    public async Task<ActionResult<VisitDto>> CompleteHandover(int id, [FromBody] CompleteHandoverDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.GateRegistered)
        {
            return Conflict(new { message = "Only visits awaiting reception handover can be completed this way." });
        }

        var hostEmployee = await _context.Employees.FirstOrDefaultAsync(employee => employee.Id == request.HostEmployeeId);
        if (hostEmployee is null)
        {
            return BadRequest(new { message = "Host employee does not exist." });
        }

        var departmentExists = await _context.Departments.AnyAsync(department => department.Id == request.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        var now = DateTime.UtcNow;
        var proposedDuration = VisitDurationPolicy.TryGetProposedDuration(request.Purpose, hostEmployee.Position);
        var hasBadge = !string.IsNullOrWhiteSpace(request.BadgeNumber);

        visit.HostEmployeeId = request.HostEmployeeId;
        visit.DepartmentId = request.DepartmentId;
        visit.Purpose = request.Purpose;
        visit.PurposeDescription = request.PurposeDescription;
        visit.BadgeNumber = request.BadgeNumber;
        if (!string.IsNullOrWhiteSpace(request.AttachmentUrl))
        {
            visit.AttachmentUrl = request.AttachmentUrl;
        }
        visit.ExpectedDepartureTime ??= proposedDuration.HasValue ? visit.ArrivalTime.Add(proposedDuration.Value) : null;
        if (hasBadge)
        {
            visit.BadgeStatus = BadgeStatus.Issued;
            visit.BadgeIssuedAt = now;
        }

        AppendHistory(visit, VisitStatus.Registered, visit.CheckedInById, "Reception completed handover.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, visit.CheckedInById, "Reception completed gate handover.");

        if (!string.IsNullOrWhiteSpace(visit.Visitor.Email))
        {
            try
            {
                var eastAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
                var arrivalDisplay = TimeZoneInfo.ConvertTimeFromUtc(visit.ArrivalTime, eastAfricaTimeZone);
                var expectedDepartureDisplay = visit.ExpectedDepartureTime.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(visit.ExpectedDepartureTime.Value, eastAfricaTimeZone).ToString("yyyy-MM-dd HH:mm:ss")
                    : "Not specified";

                var subject = "Visit registration successful";

                var body = $"""
                    <div style="margin:0;padding:32px 16px;background-color:#f4f7fb;font-family:Arial,Helvetica,sans-serif;">
                        <div style="max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden;box-shadow:0 6px 18px rgba(15,76,129,0.08);">
                            <div style="padding:28px 24px 18px 24px;text-align:center;background:linear-gradient(180deg,#ffffff 0%,#f8fbff 100%);border-bottom:1px solid #eef2f7;">
                                <img src="https://zdqixwcixanigsaucwek.supabase.co/storage/v1/object/public/Attachments/magerp-logo.svg" alt="MagERP Logo" style="max-width:160px;width:100%;height:auto;display:block;margin:0 auto 16px auto;" />
                                <div style="font-size:12px;letter-spacing:1.2px;text-transform:uppercase;color:#0f4c81;font-weight:700;">MagERP VMS</div>
                                <h2 style="margin:10px 0 0 0;font-size:24px;line-height:1.3;color:#111827;">Visit Registration Successful</h2>
                            </div>

                            <div style="padding:28px 24px;color:#1f2937;line-height:1.7;">
                                <p style="margin:0 0 14px 0;font-size:16px;color:#111827;">
                                    <strong>{visit.Visitor.FullName}</strong>, Thank you for using MagERP.
                                </p>

                                <p style="margin:0 0 22px 0;font-size:15px;color:#4b5563;">
                                    Your visit registration was completed successfully. Please find your registration details below.
                                </p>

                                <div style="background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:12px;padding:18px 16px;margin-bottom:22px;">
                                    <p style="margin:0 0 12px 0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Registration ID:</strong> {visit.VisitNumber}
                                    </p>
                                    <p style="margin:0 0 12px 0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Registered At :</strong> {arrivalDisplay:yyyy-MM-dd HH:mm:ss}
                                    </p>
                                    <p style="margin:0;font-size:15px;color:#374151;">
                                        <strong style="color:#111827;">Expected Departure :</strong> {expectedDepartureDisplay}
                                    </p>
                                </div>

                                <p style="margin:0;font-size:14px;color:#6b7280;">
                                    Please keep this email for your records. If you need any assistance, kindly contact the administrator.
                                </p>
                            </div>

                            <div style="padding:16px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;text-align:center;">
                                <p style="margin:0;font-size:13px;color:#6b7280;">Powered by <strong style="color:#0f4c81;">MagERP VMS</strong></p>
                            </div>
                        </div>
                    </div>
                """;

                await _emailService.SendEmailAsync(visit.Visitor.Email, subject, body);
                _logger.LogInformation("Registration email successfully sent for visit {VisitId} to {Email}.", visit.Id, visit.Visitor.Email);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send registration email for visit {VisitId} to {Email}.", visit.Id, visit.Visitor.Email);
            }
        }

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPatch("{id:int}/gate-details")]
    public async Task<ActionResult<VisitDto>> UpdateGateDetails(int id, [FromBody] UpdateGateDetailsDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (ClosedStatuses.Contains(visit.Status))
        {
            return Conflict(new { message = "This visit is already closed out and can no longer be edited." });
        }

        visit.VehicleModel = request.VehicleModel;
        visit.VehiclePlateNumber = request.VehiclePlateNumber;
        visit.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetAll([FromQuery] VisitStatus? status = null)
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Where(visit => status == null || visit.Status == status)
            .Include(visit => visit.Visitor)
            .Include(visit => visit.HostEmployee).ThenInclude(employee => employee!.Department)
            .Include(visit => visit.Department)
            .Include(visit => visit.ParkingReservations).ThenInclude(reservation => reservation.Slot)
            .Include(visit => visit.Items)
            .Include(visit => visit.Equipment)
            .OrderByDescending(visit => visit.ArrivalTime)
            .Select(visit => VisitMappings.ToVisitDto(visit))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitDto>> GetById(int id)
    {
        var visit = await GetVisitEntity(id, tracking: false);
        return visit is null ? NotFound() : Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IEnumerable<VisitStatusHistoryDto>>> GetHistory(int id)
    {
        var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == id);
        if (!visitExists)
        {
            return NotFound();
        }

        var history = await _context.VisitStatusHistories
            .AsNoTracking()
            .Where(entry => entry.VisitId == id)
            .OrderBy(entry => entry.ChangedAt)
            .Select(entry => new VisitStatusHistoryDto
            {
                Id = entry.Id,
                FromStatus = entry.FromStatus,
                ToStatus = entry.ToStatus,
                ChangedByUserId = entry.ChangedByUserId,
                ChangedAt = entry.ChangedAt,
                Remarks = entry.Remarks
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPut("{id:int}/notify-host")]
    public async Task<ActionResult<VisitDto>> NotifyHost(int id, [FromBody] VisitNotifyHostDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.Registered)
        {
            return Conflict(new { message = "Only registered visits can be sent to the host for acknowledgement." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        AppendHistory(visit, VisitStatus.WaitingForHost, request.ChangedByUserId, request.Remarks);
        QueueNotification(visit, NotificationType.VisitorArrived, "Visitor waiting",
            $"{visit.Visitor.FullName} has arrived and is waiting for you.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.NotificationSent, nameof(Visit), visit.Id, request.ChangedByUserId, "Host notified of visitor arrival.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/host-acknowledge")]
    public async Task<ActionResult<VisitDto>> HostAcknowledge(int id, [FromBody] VisitHostAcknowledgeDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.WaitingForHost)
        {
            return Conflict(new { message = "Only visits waiting for host can be acknowledged." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        visit.HostAcknowledgedAt = DateTime.UtcNow;
        if (request.StaffAvailabilityStatus.HasValue)
        {
            visit.StaffAvailabilityStatus = request.StaffAvailabilityStatus;
        }

        AppendHistory(visit, VisitStatus.HostAcknowledged, request.ChangedByUserId, request.Remarks);
        QueueNotification(visit, NotificationType.HostAcknowledged, "Host acknowledged",
            $"Host has acknowledged {visit.Visitor.FullName}.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Host acknowledged visitor.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/deny")]
    public async Task<ActionResult<VisitDto>> Deny(int id, [FromBody] VisitDenyDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.WaitingForHost)
        {
            return Conflict(new { message = "Only visits waiting for host can be denied." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        visit.ClosedAt = DateTime.UtcNow;
        AppendHistory(visit, VisitStatus.Denied, request.ChangedByUserId, request.Remarks);

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Host denied visit.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/mark-attended")]
    public async Task<ActionResult<VisitDto>> MarkAttended(int id, [FromBody] VisitMarkAttendedDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.HostAcknowledged)
        {
            return Conflict(new { message = "Only acknowledged visits can be marked as attended." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        AppendHistory(visit, VisitStatus.Attended, request.ChangedByUserId, request.Remarks);

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Visit marked as attended.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/host-complete")]
    public async Task<ActionResult<VisitDto>> HostComplete(int id, [FromBody] VisitHostCompleteDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.Attended)
        {
            return Conflict(new { message = "Only attended visits can be marked host-complete." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        visit.HostCompletedAt = DateTime.UtcNow;
        AppendHistory(visit, VisitStatus.AwaitingExit, request.ChangedByUserId, request.Remarks);
        QueueNotification(visit, NotificationType.CheckoutRequired, "Checkout required",
            $"{visit.Visitor.FullName} is ready to exit and needs to be checked out.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Host completed the engagement.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/checkout")]
    public async Task<ActionResult<VisitDto>> CheckOut(int id, [FromBody] VisitCheckOutDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.AwaitingExit)
        {
            return Conflict(new { message = "Only visits awaiting exit can be checked out." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.CheckedOutById);
        if (!actorExists)
        {
            return BadRequest(new { message = "Check-out user does not exist." });
        }

        var now = DateTime.UtcNow;
        visit.CheckedOutById = request.CheckedOutById;
        visit.DepartureTime = now;
        visit.VisitorExitAcknowledgedAt = now;
        visit.ExitSignatureReference = request.ExitSignatureReference;
        visit.Remarks = request.Remarks;

        var badgeReturned = request.BadgeReturned && !string.IsNullOrWhiteSpace(visit.BadgeNumber);
        if (badgeReturned)
        {
            visit.BadgeReturnedAt = now;
            visit.BadgeStatus = BadgeStatus.Available;
        }

        AppendHistory(visit, VisitStatus.Completed, request.CheckedOutById, request.Remarks);
        QueueNotification(visit, NotificationType.VisitCompleted, "Visit completed",
            $"{visit.Visitor.FullName} has checked out.");

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(AuditAction.CheckOut, nameof(Visit), visit.Id, request.CheckedOutById, "Visitor checked out.");
        if (badgeReturned)
        {
            await _auditLog.LogAsync(AuditAction.BadgeReturned, nameof(Visit), visit.Id, request.CheckedOutById, $"Badge {visit.BadgeNumber} returned.");
        }

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/close")]
    public async Task<ActionResult<VisitDto>> Close(int id, [FromBody] VisitCloseDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.Completed)
        {
            return Conflict(new { message = "Only completed visits can be closed." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        visit.ClosedAt = DateTime.UtcNow;
        AppendHistory(visit, VisitStatus.Closed, request.ChangedByUserId, request.Remarks);
        QueueNotification(visit, NotificationType.VisitClosed, "Visit closed",
            $"Visit {visit.VisitNumber} has been closed.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Visit closed.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<VisitDto>> Cancel(int id, [FromBody] VisitCancelDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (!CancellableStatuses.Contains(visit.Status))
        {
            return Conflict(new { message = "Visit can no longer be cancelled at its current status." });
        }

        var actorExists = await _context.Users.AnyAsync(user => user.Id == request.ChangedByUserId);
        if (!actorExists)
        {
            return BadRequest(new { message = "Acting user does not exist." });
        }

        visit.ClosedAt = DateTime.UtcNow;
        AppendHistory(visit, VisitStatus.Cancelled, request.ChangedByUserId, request.Remarks);
        QueueNotification(visit, NotificationType.VisitCancelled, "Visit cancelled",
            $"Visit {visit.VisitNumber} was cancelled.");

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.StatusChanged, nameof(Visit), visit.Id, request.ChangedByUserId, "Visit cancelled.");

        return Ok(VisitMappings.ToVisitDto(visit));
    }

    private async Task<string> GenerateVisitNumberAsync(DateTime now)
    {
        var prefix = $"V-{now:yyyyMMdd}-";
        var countToday = await _context.Visits.CountAsync(visit => visit.VisitNumber.StartsWith(prefix));
        return $"{prefix}{(countToday + 1):D4}";
    }

    private static void AppendHistory(Visit visit, VisitStatus toStatus, int? changedByUserId, string? remarks)
    {
        var changedAt = DateTime.UtcNow;
        visit.StatusHistory.Add(new VisitStatusHistory
        {
            VisitId = visit.Id,
            FromStatus = visit.Status,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = changedAt,
            Remarks = remarks
        });

        visit.Status = toStatus;
        visit.UpdatedAt = changedAt;
    }

    private static void QueueNotification(Visit visit, NotificationType type, string title, string message, NotificationChannel channel = NotificationChannel.InApp)
    {
        visit.Notifications.Add(new Notification
        {
            VisitId = visit.Id,
            RecipientEmployeeId = visit.HostEmployeeId,
            Type = type,
            Channel = channel,
            Title = title,
            Message = message,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    private Task<Visit?> GetVisitEntity(int id, bool tracking = true)
    {
        IQueryable<Visit> query = _context.Visits;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(visit => visit.Visitor)
            .Include(visit => visit.HostEmployee).ThenInclude(employee => employee!.Department)
            .Include(visit => visit.Department)
            .Include(visit => visit.ParkingReservations).ThenInclude(reservation => reservation.Slot)
            .Include(visit => visit.Items)
            .Include(visit => visit.Equipment)
            .FirstOrDefaultAsync(visit => visit.Id == id);
    }
}

internal static class VisitMappings
{
    public static VisitDto ToVisitDto(Visit visit)
    {
        return new VisitDto
        {
            Id = visit.Id,
            VisitNumber = visit.VisitNumber,
            VisitorId = visit.VisitorId,
            HostEmployeeId = visit.HostEmployeeId,
            DepartmentId = visit.DepartmentId,
            Purpose = visit.Purpose,
            PurposeDescription = visit.PurposeDescription,
            Status = visit.Status,
            StaffAvailabilityStatus = visit.StaffAvailabilityStatus,
            ArrivalTime = visit.ArrivalTime,
            ExpectedDepartureTime = visit.ExpectedDepartureTime,
            DepartureTime = visit.DepartureTime,
            VehicleModel = visit.VehicleModel,
            VehiclePlateNumber = visit.VehiclePlateNumber,
            BadgeNumber = visit.BadgeNumber,
            BadgeStatus = visit.BadgeStatus,
            BadgeIssuedAt = visit.BadgeIssuedAt,
            BadgeReturnedAt = visit.BadgeReturnedAt,
            HostAcknowledgedAt = visit.HostAcknowledgedAt,
            HostCompletedAt = visit.HostCompletedAt,
            VisitorExitAcknowledgedAt = visit.VisitorExitAcknowledgedAt,
            ExitSignatureReference = visit.ExitSignatureReference,
            CheckedInById = visit.CheckedInById,
            CheckedOutById = visit.CheckedOutById,
            Remarks = visit.Remarks,
            AttachmentUrl = visit.AttachmentUrl,
            CreatedAt = visit.CreatedAt,
            UpdatedAt = visit.UpdatedAt,
            ClosedAt = visit.ClosedAt,
            ParkingReservations = visit.ParkingReservations
                .OrderByDescending(reservation => reservation.Status == ParkingReservationStatus.Reserved)
                .ThenByDescending(reservation => reservation.ReservedAt ?? reservation.CreatedAt)
                .Select(reservation => new VisitParkingReservationDto
                {
                    Id = reservation.Id,
                    SlotId = reservation.SlotId,
                    SlotCode = reservation.Slot.Code,
                    Zone = reservation.Slot.Zone,
                    VehiclePlate = reservation.VehiclePlate,
                    Status = reservation.Status,
                    ReservedAt = reservation.ReservedAt,
                    ReleasedAt = reservation.ReleasedAt
                })
                .ToList(),
            Items = visit.Items
                .OrderBy(item => item.CreatedAt)
                .Select(item => new VisitItemDto
                {
                    Id = item.Id,
                    VisitId = item.VisitId,
                    ItemName = item.ItemName,
                    ItemType = item.ItemType,
                    MovementType = item.MovementType,
                    Description = item.Description,
                    SerialNumber = item.SerialNumber,
                    Quantity = item.Quantity,
                    Remarks = item.Remarks,
                    CreatedAt = item.CreatedAt
                })
                .ToList(),
            Visitor = new VisitorDto
            {
                Id = visit.Visitor.Id,
                FullName = visit.Visitor.FullName,
                PhoneNumber = visit.Visitor.PhoneNumber,
                Email = visit.Visitor.Email,
                IdentificationType = visit.Visitor.IdentificationType,
                IdentificationNumber = visit.Visitor.IdentificationNumber,
                Organization = visit.Visitor.Organization,
                PhotoUrl = visit.Visitor.PhotoUrl,
                IsActive = visit.Visitor.IsActive,
                CreatedAt = visit.Visitor.CreatedAt,
                UpdatedAt = visit.Visitor.UpdatedAt
            },
            HostEmployee = visit.HostEmployee is null ? null : new EmployeeDto
            {
                Id = visit.HostEmployee.Id,
                EmployeeNumber = visit.HostEmployee.EmployeeNumber,
                FullName = visit.HostEmployee.FullName,
                Email = visit.HostEmployee.Email,
                PhoneNumber = visit.HostEmployee.PhoneNumber,
                Position = visit.HostEmployee.Position,
                DepartmentId = visit.HostEmployee.DepartmentId,
                DepartmentName = visit.HostEmployee.Department?.Name ?? string.Empty,
                IsActive = visit.HostEmployee.IsActive,
                CreatedAt = visit.HostEmployee.CreatedAt,
                UpdatedAt = visit.HostEmployee.UpdatedAt
            },
            Department = visit.Department is null ? null : new DepartmentDto
            {
                Id = visit.Department.Id,
                Code = visit.Department.Code,
                Name = visit.Department.Name,
                Description = visit.Department.Description,
                IsActive = visit.Department.IsActive,
                CreatedAt = visit.Department.CreatedAt,
                UpdatedAt = visit.Department.UpdatedAt
            },
            Equipment = visit.Equipment is null ? null : new VisitEquipmentDto
            {
                Id = visit.Equipment.Id,
                VisitId = visit.Equipment.VisitId,
                HasLaptop = visit.Equipment.HasLaptop,
                DeviceType = visit.Equipment.DeviceType,
                DeviceBrand = visit.Equipment.DeviceBrand,
                AssetTag = visit.Equipment.AssetTag,
                PcConfirmedReturned = visit.Equipment.PcConfirmedReturned
            }
        };
    }
}
