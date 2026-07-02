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
        var now = DateTime.UtcNow;

        var office = new Office
        {
            Name = request.Name,
            Floor = request.Floor,
            OfficeCode = request.OfficeCode,
            CreatedAt = now,
            UpdatedAt = now
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
            .OrderByDescending(office => office.Id)
            .Select(office => ToOfficeDto(office))
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
        var officeExists = await _context.Offices.AnyAsync(office => office.Id == id);
        if (!officeExists)
        {
            return NotFound();
        }

        var visits = await _context.Visits
            .AsNoTracking()
            .Include(visit => visit.Visitor)
            .Include(visit => visit.Office)
            .Include(visit => visit.CheckIns)
            .Include(visit => visit.CheckOuts)
            .Where(visit => visit.OfficeId == id)
            .OrderByDescending(visit => visit.VisitDate)
            .Select(visit => VisitMappings.ToVisitDto(visit))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OfficeDto>> Update(int id, [FromBody] UpdateOfficeDto request)
    {
        var office = await _context.Offices.FirstOrDefaultAsync(existingOffice => existingOffice.Id == id);
        if (office is null)
        {
            return NotFound();
        }

        office.Name = request.Name;
        office.Floor = request.Floor;
        office.OfficeCode = request.OfficeCode;
        office.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToOfficeDto(office));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var office = await _context.Offices.FirstOrDefaultAsync(existingOffice => existingOffice.Id == id);
        if (office is null)
        {
            return NotFound();
        }

        var hasVisits = await _context.Visits.AnyAsync(visit => visit.OfficeId == id);
        var hasDepartments = await _context.Departments.AnyAsync(department => department.OfficeId == id);
        if (hasVisits || hasDepartments)
        {
            return Conflict(new { message = "Cannot delete office with related visits or departments." });
        }

        _context.Offices.Remove(office);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static OfficeDto ToOfficeDto(Office office) => new()
    {
        Id = office.Id,
        Name = office.Name,
        Floor = office.Floor,
        OfficeCode = office.OfficeCode,
        CreatedAt = office.CreatedAt,
        UpdatedAt = office.UpdatedAt
    };
}
