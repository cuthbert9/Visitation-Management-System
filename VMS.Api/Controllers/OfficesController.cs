using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OfficesController : ControllerBase
{
    private readonly AppDbContext _context;

    public OfficesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<OfficeDto>> Create([FromBody] CreateOfficeDto request)
    {
        var office = new Office
        {
            Name = request.Name,
            Location = request.Location,
            Status = request.Status
        };

        _context.Offices.Add(office);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = office.Id }, ToOfficeDto(office));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OfficeDto>>> GetAll()
    {
        var offices = await _context.Offices
            .AsNoTracking()
            .OrderByDescending(o => o.Id)
            .Select(o => ToOfficeDto(o))
            .ToListAsync();

        return Ok(offices);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OfficeDto>> GetById(int id)
    {
        var office = await _context.Offices.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return office is null ? NotFound() : Ok(ToOfficeDto(office));
    }

    [HttpGet("{id:int}/visits")]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetOfficeVisits(int id)
    {
        var officeExists = await _context.Offices.AnyAsync(o => o.Id == id);
        if (!officeExists)
        {
            return NotFound();
        }

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Visitor)
            .Include(v => v.Office)
            .Where(v => v.OfficeId == id)
            .OrderByDescending(v => v.VisitDate)
            .Select(v => VisitMappings.ToVisitDto(v))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OfficeDto>> Update(int id, [FromBody] UpdateOfficeDto request)
    {
        var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == id);
        if (office is null)
        {
            return NotFound();
        }

        office.Name = request.Name;
        office.Location = request.Location;
        office.Status = request.Status;

        await _context.SaveChangesAsync();
        return Ok(ToOfficeDto(office));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == id);
        if (office is null)
        {
            return NotFound();
        }

        var hasVisits = await _context.Visits.AnyAsync(v => v.OfficeId == id);
        if (hasVisits)
        {
            return Conflict(new { message = "Cannot delete office with existing visits." });
        }

        _context.Offices.Remove(office);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static OfficeDto ToOfficeDto(Office office) => new()
    {
        Id = office.Id,
        Name = office.Name,
        Location = office.Location,
        Status = office.Status
    };
}
