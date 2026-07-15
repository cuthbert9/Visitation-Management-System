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
        var eastAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Nairobi");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, eastAfricaTimeZone);

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
                <div style="margin:0;padding:32px 16px;background-color:#f4f7fb;font-family:Arial,Helvetica,sans-serif;">
                    <div style="max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;overflow:hidden;box-shadow:0 6px 18px rgba(15,76,129,0.08);">
                        <div style="padding:28px 24px 18px 24px;text-align:center;background:linear-gradient(180deg,#ffffff 0%,#f8fbff 100%);border-bottom:1px solid #eef2f7;">
                            <img src="https://zdqixwcixanigsaucwek.supabase.co/storage/v1/object/public/Attachments/magerp-logo.svg" alt="MagERP Logo" style="max-width:160px;width:100%;height:auto;display:block;margin:0 auto 16px auto;" />
                            <div style="font-size:12px;letter-spacing:1.2px;text-transform:uppercase;color:#0f4c81;font-weight:700;">MagERP VMS</div>
                            <h2 style="margin:10px 0 0 0;font-size:24px;line-height:1.3;color:#111827;">Visitor Registration Successful</h2>
                        </div>

                        <div style="padding:28px 24px;color:#1f2937;line-height:1.7;">
                            <p style="margin:0 0 14px 0;font-size:16px;color:#111827;">
                                <strong>{visitor.FullName}</strong>, Thank you for using MagERP.
                            </p>

                            <p style="margin:0 0 22px 0;font-size:15px;color:#4b5563;">
                                Your visitor registration was completed successfully. Please find your registration details below.
                            </p>

                            <div style="background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:12px;padding:18px 16px;margin-bottom:22px;">
                                <p style="margin:0 0 12px 0;font-size:15px;color:#374151;">
                                    <strong style="color:#111827;">Registration ID:</strong> {visitor.Id}
                                </p>
                                <p style="margin:0;font-size:15px;color:#374151;">
                                    <strong style="color:#111827;">Registered At :</strong> {visitor.CreatedAt:yyyy-MM-dd HH:mm:ss}
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
