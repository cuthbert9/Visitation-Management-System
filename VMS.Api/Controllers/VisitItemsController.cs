using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<VisitItemDto>> Create([FromBody] CreateVisitItemDto request)
    {
        var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == request.VisitId);
        if (!visitExists)
        {
            return BadRequest(new { message = "Visit does not exist." });
        }

        var item = new VisitItem
        {
            VisitId = request.VisitId,
            ItemName = request.ItemName,
            ItemType = request.ItemType,
            MovementType = request.MovementType,
            Description = request.Description,
            SerialNumber = request.SerialNumber,
            Quantity = request.Quantity,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        _context.VisitItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToVisitItemDto(item));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VisitItemDto>>> GetAll()
    {
        var items = await _context.VisitItems
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => ToVisitItemDto(item))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitItemDto>> GetById(int id)
    {
        var item = await _context.VisitItems.AsNoTracking().FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        return item is null ? NotFound() : Ok(ToVisitItemDto(item));
    }

    [HttpGet("visit/{visitId:int}")]
    public async Task<ActionResult<IEnumerable<VisitItemDto>>> GetByVisit(int visitId)
    {
        var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == visitId);
        if (!visitExists)
        {
            return NotFound();
        }

        var items = await _context.VisitItems
            .AsNoTracking()
            .Where(item => item.VisitId == visitId)
            .OrderBy(item => item.CreatedAt)
            .Select(item => ToVisitItemDto(item))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VisitItemDto>> Update(int id, [FromBody] UpdateVisitItemDto request)
    {
        var item = await _context.VisitItems.FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.ItemName = request.ItemName;
        item.ItemType = request.ItemType;
        item.MovementType = request.MovementType;
        item.Description = request.Description;
        item.SerialNumber = request.SerialNumber;
        item.Quantity = request.Quantity;
        item.Remarks = request.Remarks;

        await _context.SaveChangesAsync();
        return Ok(ToVisitItemDto(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.VisitItems.FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _context.VisitItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static VisitItemDto ToVisitItemDto(VisitItem item) => new()
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
    };
}
