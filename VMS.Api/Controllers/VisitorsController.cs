using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<VisitorsController> _logger;

    public VisitorsController(AppDbContext context, IEmailService emailService, ILogger<VisitorsController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<VisitorDto>> Create([FromBody] CreateVisitorDto request)
    {
        var now = DateTime.UtcNow;

        var visitor = new Visitor
        {
            FullName = request.FullName,
            Phone = request.Phone,
            NationalId = request.NationalId,
            Company = request.Company,
            VehiclePlate = request.VehiclePlate,
            PhotoUrl = request.PhotoUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Visitors.Add(visitor);
        await _context.SaveChangesAsync();

        try
        {
            var subject = "Visitor registration successful"; 
            var body = $"""
                <div style="margin:0;padding:0;background-color:#f4f7fb;font-family:Arial,Helvetica,sans-serif;">
                    <div style="max-width:600px;margin:30px auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:12px;overflow:hidden;box-shadow:0 4px 12px rgba(0,0,0,0.06);">
                        <div style="background-color:#0f4c81;padding:20px 24px;color:#ffffff;">
                            <h2 style="margin:0;font-size:24px;">Visitor Registration Successful</h2>
                        </div>

                        <div style="padding:24px;color:#1f2937;line-height:1.6;">
                            <p style="margin:0 0 16px 0;font-size:16px;">
                                <strong>{visitor.FullName}</strong>, Thank you for using our VMS.
                            </p>

                            <p style="margin:0 0 20px 0;font-size:15px;color:#4b5563;">
                                Your visitor registration was completed successfully. Below are your Registration details:
                            </p>

                            <div style="background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;padding:16px;margin-bottom:20px;">
                                <p style="margin:0 0 10px 0;font-size:15px;">
                                    <strong>Registration ID:</strong> {visitor.Id}
                                </p>
                                <p style="margin:0;font-size:15px;">
                                    <strong>Registered At (UTC):</strong> {visitor.CreatedAt:yyyy-MM-dd HH:mm:ss}
                                </p>
                            </div>

                            <p style="margin:0;font-size:14px;color:#6b7280;">
                                Please keep this information for your reference. If you need any assistance, contact the administrator.
                            </p>
                        </div>

                        <div style="padding:16px 24px;background-color:#f9fafb;border-top:1px solid #e5e7eb;font-size:13px;color:#6b7280;text-align:center;">
                            Visitor Management System
                        </div>
                    </div>
                </div>
            """;

            await _emailService.SendEmailAsync(visitor.FullName, subject, body);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send registration email for visitor {VisitorId} to {Recipient}.", visitor.Id, visitor.FullName);
        }

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
            .Include(visit => visit.Office)
            .Include(visit => visit.CheckIns)
            .Include(visit => visit.CheckOuts)
            .Where(visit => visit.VisitorId == id)
            .OrderByDescending(visit => visit.VisitDate)
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
        visitor.Phone = request.Phone;
        visitor.NationalId = request.NationalId;
        visitor.Company = request.Company;
        visitor.VehiclePlate = request.VehiclePlate;
        visitor.PhotoUrl = request.PhotoUrl;
        visitor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
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
        return NoContent();
    }

    private static VisitorDto ToVisitorDto(Visitor visitor) => new()
    {
        Id = visitor.Id,
        FullName = visitor.FullName,
        Phone = visitor.Phone,
        NationalId = visitor.NationalId,
        Company = visitor.Company,
        VehiclePlate = visitor.VehiclePlate,
        PhotoUrl = visitor.PhotoUrl,
        CreatedAt = visitor.CreatedAt,
        UpdatedAt = visitor.UpdatedAt
    };
}
