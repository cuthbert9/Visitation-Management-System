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
        var visitor = new Visitor
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            NationalId = request.NationalId,
            CreatedAt = DateTime.UtcNow
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
            .OrderByDescending(v => v.Id)
            .Select(v => ToVisitorDto(v))
            .ToListAsync();

        return Ok(visitors);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitorDto>> GetById(int id)
    {
        var visitor = await _context.Visitors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        return visitor is null ? NotFound() : Ok(ToVisitorDto(visitor));
    }

    [HttpGet("{id:int}/visits")]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetVisitorVisits(int id)
    {
        var visitorExists = await _context.Visitors.AnyAsync(v => v.Id == id);
        if (!visitorExists)
        {
            return NotFound();
        }

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Visitor)
            .Include(v => v.Office)
            .Where(v => v.VisitorId == id)
            .OrderByDescending(v => v.VisitDate)
            .Select(v => VisitMappings.ToVisitDto(v))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VisitorDto>> Update(int id, [FromBody] UpdateVisitorDto request)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(v => v.Id == id);
        if (visitor is null)
        {
            return NotFound();
        }

        visitor.FullName = request.FullName;
        visitor.PhoneNumber = request.PhoneNumber;
        visitor.NationalId = request.NationalId;

        await _context.SaveChangesAsync();
        return Ok(ToVisitorDto(visitor));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var visitor = await _context.Visitors.FirstOrDefaultAsync(v => v.Id == id);
        if (visitor is null)
        {
            return NotFound();
        }

        var hasVisits = await _context.Visits.AnyAsync(v => v.VisitorId == id);
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
        PhoneNumber = visitor.PhoneNumber,
        NationalId = visitor.NationalId,
        CreatedAt = visitor.CreatedAt
    };
}
