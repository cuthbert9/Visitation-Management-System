using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<VisitDto>> Create([FromBody] VisitCreateDto request)
    {
        var visitorExists = await _context.Visitors.AnyAsync(v => v.Id == request.VisitorId);
        if (!visitorExists)
        {
            return BadRequest(new { message = "Visitor does not exist." });
        }

        var officeExists = await _context.Offices.AnyAsync(o => o.Id == request.OfficeId);
        if (!officeExists)
        {
            return BadRequest(new { message = "Office does not exist." });
        }

        var visitDate = request.VisitDate.Date;
        var duplicateActiveVisit = await _context.Visits.AnyAsync(v =>
            v.VisitorId == request.VisitorId &&
            v.VisitDate.Date == visitDate &&
            (v.Status == VisitStatus.Pending || v.Status == VisitStatus.Approved));

        if (duplicateActiveVisit)
        {
            return Conflict(new { message = "Visitor already has an active visit on this date." });
        }

        var visit = new Visit
        {
            VisitorId = request.VisitorId,
            OfficeId = request.OfficeId,
            VisitDate = request.VisitDate,
            Purpose = request.Purpose,
            Status = VisitStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();

        var createdVisit = await GetVisitEntity(visit.Id);
        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, VisitMappings.ToVisitDto(createdVisit!));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetAll()
    {
        var visits = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Visitor)
            .Include(v => v.Office)
            .OrderByDescending(v => v.VisitDate)
            .Select(v => VisitMappings.ToVisitDto(v))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitDto>> GetById(int id)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Include(v => v.Visitor)
            .Include(v => v.Office)
            .FirstOrDefaultAsync(v => v.Id == id);

        return visit is null ? NotFound() : Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult<VisitDto>> Approve(int id)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        visit.Status = VisitStatus.Approved;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/reject")]
    public async Task<ActionResult<VisitDto>> Reject(int id)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        visit.Status = VisitStatus.Rejected;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/checkin")]
    public async Task<ActionResult<VisitDto>> CheckIn(int id)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.Approved)
        {
            return Conflict(new { message = "Only approved visits can be checked in." });
        }

        visit.CheckInTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/checkout")]
    public async Task<ActionResult<VisitDto>> CheckOut(int id)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (!visit.CheckInTime.HasValue)
        {
            return Conflict(new { message = "Visit must be checked in before checkout." });
        }

        visit.CheckOutTime = DateTime.UtcNow;
        visit.Status = VisitStatus.Completed;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    private Task<Visit?> GetVisitEntity(int id)
    {
        return _context.Visits
            .Include(v => v.Visitor)
            .Include(v => v.Office)
            .FirstOrDefaultAsync(v => v.Id == id);
    }
}

internal static class VisitMappings
{
    public static VisitDto ToVisitDto(Visit visit) => new()
    {
        Id = visit.Id,
        VisitorId = visit.VisitorId,
        OfficeId = visit.OfficeId,
        VisitDate = visit.VisitDate,
        CheckInTime = visit.CheckInTime,
        CheckOutTime = visit.CheckOutTime,
        Status = visit.Status,
        Purpose = visit.Purpose,
        CreatedAt = visit.CreatedAt,
        Visitor = new VisitorDto
        {
            Id = visit.Visitor.Id,
            FullName = visit.Visitor.FullName,
            PhoneNumber = visit.Visitor.PhoneNumber,
            NationalId = visit.Visitor.NationalId,
            CreatedAt = visit.Visitor.CreatedAt
        },
        Office = new OfficeDto
        {
            Id = visit.Office.Id,
            Name = visit.Office.Name,
            Location = visit.Office.Location,
            Status = visit.Office.Status
        }
    };
}
