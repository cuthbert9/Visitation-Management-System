using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitorsController(AppDbContext context)
    {
        _context = context;
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
