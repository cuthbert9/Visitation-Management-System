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
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        AppDbContext context,
        IEmailService emailService,
        IAuditLogService auditLog,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _emailService = emailService;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationDto request)
    {
        if (request.VisitId.HasValue)
        {
            var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == request.VisitId.Value);
            if (!visitExists)
            {
                return BadRequest(new { message = "Visit does not exist." });
            }
        }

        Employee? recipientEmployee = null;
        if (request.RecipientEmployeeId.HasValue)
        {
            recipientEmployee = await _context.Employees.FirstOrDefaultAsync(employee => employee.Id == request.RecipientEmployeeId.Value);
            if (recipientEmployee is null)
            {
                return BadRequest(new { message = "Recipient employee does not exist." });
            }
        }

        if (request.RecipientUserId.HasValue)
        {
            var recipientUserExists = await _context.Users.AnyAsync(user => user.Id == request.RecipientUserId.Value);
            if (!recipientUserExists)
            {
                return BadRequest(new { message = "Recipient user does not exist." });
            }
        }

        var notification = new Notification
        {
            VisitId = request.VisitId,
            RecipientEmployeeId = request.RecipientEmployeeId,
            RecipientUserId = request.RecipientUserId,
            Type = request.Type,
            Channel = request.Channel,
            Title = request.Title,
            Message = request.Message,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        if (notification.Channel == NotificationChannel.Email && !string.IsNullOrWhiteSpace(recipientEmployee?.Email))
        {
            try
            {
                await _emailService.SendEmailAsync(recipientEmployee.Email, notification.Title, notification.Message);
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
            }
            catch (Exception exception)
            {
                notification.Status = NotificationStatus.Failed;
                notification.FailedAt = DateTime.UtcNow;
                notification.RetryCount += 1;
                notification.ErrorMessage = exception.Message;
                _logger.LogError(exception, "Failed to send notification {NotificationId}.", notification.Id);
            }

            await _context.SaveChangesAsync();
        }

        await _auditLog.LogAsync(AuditAction.NotificationSent, nameof(Notification), notification.Id, request.RecipientUserId, notification.Title);

        return CreatedAtAction(nameof(GetById), new { id = notification.Id }, ToNotificationDto(notification));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetAll()
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => ToNotificationDto(notification))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NotificationDto>> GetById(int id)
    {
        var notification = await _context.Notifications.AsNoTracking().FirstOrDefaultAsync(existingNotification => existingNotification.Id == id);
        return notification is null ? NotFound() : Ok(ToNotificationDto(notification));
    }

    [HttpGet("visit/{visitId:int}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByVisit(int visitId)
    {
        var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == visitId);
        if (!visitExists)
        {
            return NotFound();
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.VisitId == visitId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Select(notification => ToNotificationDto(notification))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPut("{id:int}/read")]
    public async Task<ActionResult<NotificationDto>> MarkRead(int id)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(existingNotification => existingNotification.Id == id);
        if (notification is null)
        {
            return NotFound();
        }

        notification.Status = NotificationStatus.Read;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToNotificationDto(notification));
    }

    private static NotificationDto ToNotificationDto(Notification notification) => new()
    {
        Id = notification.Id,
        VisitId = notification.VisitId,
        RecipientEmployeeId = notification.RecipientEmployeeId,
        RecipientUserId = notification.RecipientUserId,
        Type = notification.Type,
        Channel = notification.Channel,
        Title = notification.Title,
        Message = notification.Message,
        Status = notification.Status,
        CreatedAt = notification.CreatedAt,
        SentAt = notification.SentAt,
        ReadAt = notification.ReadAt,
        FailedAt = notification.FailedAt,
        RetryCount = notification.RetryCount,
        ErrorMessage = notification.ErrorMessage
    };
}
