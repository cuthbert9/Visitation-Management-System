using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
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

    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public VisitsController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<ActionResult<VisitDto>> Create([FromBody] VisitCreateDto request)
    {
        var visitorExists = await _context.Visitors.AnyAsync(visitor => visitor.Id == request.VisitorId);
        if (!visitorExists)
        {
            return BadRequest(new { message = "Visitor does not exist." });
        }

        var hostEmployeeExists = await _context.Employees.AnyAsync(employee => employee.Id == request.HostEmployeeId);
        if (!hostEmployeeExists)
        {
            return BadRequest(new { message = "Host employee does not exist." });
        }

        var departmentExists = await _context.Departments.AnyAsync(department => department.Id == request.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        var checkedInByExists = await _context.Users.AnyAsync(user => user.Id == request.CheckedInById);
        
        if (!checkedInByExists)
        {
            return BadRequest(new { message = "Checking-in user does not exist." });
        }

        var now = DateTime.UtcNow;
        var hasBadge = !string.IsNullOrWhiteSpace(request.BadgeNumber);

        var visit = new Visit
        {
            VisitNumber = await GenerateVisitNumberAsync(now),
            VisitorId = request.VisitorId,
            HostEmployeeId = request.HostEmployeeId,
            DepartmentId = request.DepartmentId,
            Purpose = request.Purpose,
            PurposeDescription = request.PurposeDescription,
            Status = VisitStatus.Registered,
            ArrivalTime = now,
            ExpectedDepartureTime = request.ExpectedDepartureTime,
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
            ToStatus = VisitStatus.Registered,
            ChangedByUserId = request.CheckedInById,
            ChangedAt = now
        });
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(AuditAction.CheckIn, nameof(Visit), visit.Id, request.CheckedInById, "Visitor registered and checked in.");
        if (hasBadge)
        {
            await _auditLog.LogAsync(AuditAction.BadgeAssigned, nameof(Visit), visit.Id, request.CheckedInById, $"Badge {visit.BadgeNumber} issued.");
        }

        var createdVisit = await GetVisitEntity(visit.Id);
        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, VisitMappings.ToVisitDto(createdVisit!));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetAll()
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Include(visit => visit.Visitor)
            .Include(visit => visit.HostEmployee).ThenInclude(employee => employee.Department)
            .Include(visit => visit.Department)
            .Include(visit => visit.ParkingReservations).ThenInclude(reservation => reservation.Slot)
            .Include(visit => visit.Items)
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
            .Include(visit => visit.HostEmployee).ThenInclude(employee => employee.Department)
            .Include(visit => visit.Department)
            .Include(visit => visit.ParkingReservations).ThenInclude(reservation => reservation.Slot)
            .Include(visit => visit.Items)
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
            HostEmployee = new EmployeeDto
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
            Department = new DepartmentDto
            {
                Id = visit.Department.Id,
                Code = visit.Department.Code,
                Name = visit.Department.Name,
                Description = visit.Department.Description,
                IsActive = visit.Department.IsActive,
                CreatedAt = visit.Department.CreatedAt,
                UpdatedAt = visit.Department.UpdatedAt
            }
        };
    }
}
