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
public class VisitorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<VisitorsController> _logger;

    public VisitorsController(
        AppDbContext context,
        IAuditLogService auditLog,
        ILogger<VisitorsController> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<VisitorDto>> Create([FromBody] CreateVisitorDto request)
    {
        _logger.LogInformation("Visitor registration started for {FullName}.", request.FullName);

        var eastAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastAfricaTimeZone);

        var visitor = new Visitor
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            IdentificationType = request.IdentificationType,
            IdentificationNumber = request.IdentificationNumber,
            Organization = request.Organization,
            PhotoUrl = request.PhotoUrl,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Visitor {VisitorId} registered successfully.", visitor.Id);

        await _auditLog.LogAsync(AuditAction.Create, nameof(Visitor), visitor.Id, null, "Visitor registered.");

        _logger.LogInformation("Audit log created for visitor {VisitorId}.", visitor.Id);

        return CreatedAtAction(nameof(GetById), new { id = visitor.Id }, ToVisitorDto(visitor));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitorDto>>> GetAll()
    {
        var visitors = await _context.Visitors
            .AsNoTracking()
            .OrderByDescending(visitor => visitor.Id)
            .Select(visitor => ToVisitorDto(visitor))
            .ToListAsync();

        return Ok(visitors);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitorDto>> GetById(int id)
    {
        var visitor = await _context.Visitors.AsNoTracking().FirstOrDefaultAsync(existingVisitor => existingVisitor.Id == id);
        return visitor is null ? NotFound() : Ok(ToVisitorDto(visitor));
    }

    [HttpGet("{id:int}/visits")]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetVisitorVisits(int id)
    {
        var visitorExists = await _context.Visitors.AnyAsync(visitor => visitor.Id == id);
        if (!visitorExists)
        {
            return NotFound();
        }

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(visit => visit.Visitor)
            .Include(visit => visit.HostEmployee).ThenInclude(employee => employee.Department)
            .Include(visit => visit.Department)
            .Include(visit => visit.ParkingReservations).ThenInclude(reservation => reservation.Slot)
            .Include(visit => visit.Items)
            .Where(visit => visit.VisitorId == id)
            .OrderByDescending(visit => visit.ArrivalTime)
            .Select(visit => VisitMappings.ToVisitDto(visit))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VisitorDto>> Update(int id, [FromBody] UpdateVisitorDto request)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(existingVisitor => existingVisitor.Id == id);
        if (visitor is null)
        {
            return NotFound();
        }

        visitor.FullName = request.FullName;
        visitor.PhoneNumber = request.PhoneNumber;
        visitor.Email = request.Email;
        visitor.IdentificationType = request.IdentificationType;
        visitor.IdentificationNumber = request.IdentificationNumber;
        visitor.Organization = request.Organization;
        visitor.PhotoUrl = request.PhotoUrl;
        visitor.IsActive = request.IsActive;
        visitor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Update, nameof(Visitor), visitor.Id, null, "Visitor updated.");

        return Ok(ToVisitorDto(visitor));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(existingVisitor => existingVisitor.Id == id);
        if (visitor is null)
        {
            return NotFound();
        }

        var hasVisits = await _context.Visits.AnyAsync(visit => visit.VisitorId == id);
        if (hasVisits)
        {
            return Conflict(new { message = "Cannot delete visitor with existing visits." });
        }

        _context.Visitors.Remove(visitor);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Delete, nameof(Visitor), id, null, "Visitor deleted.");

        return NoContent();
    }

    private static VisitorDto ToVisitorDto(Visitor visitor) => new()
    {
        Id = visitor.Id,
        FullName = visitor.FullName,
        PhoneNumber = visitor.PhoneNumber,
        Email = visitor.Email,
        IdentificationType = visitor.IdentificationType,
        IdentificationNumber = visitor.IdentificationNumber,
        Organization = visitor.Organization,
        PhotoUrl = visitor.PhotoUrl,
        IsActive = visitor.IsActive,
        CreatedAt = visitor.CreatedAt,
        UpdatedAt = visitor.UpdatedAt
    };
}
