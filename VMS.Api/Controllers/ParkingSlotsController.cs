using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingSlotsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ParkingSlotsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ParkingSlotDto>> Create([FromBody] CreateParkingSlotDto request)
    {
        var codeExists = await _context.ParkingSlots.AnyAsync(slot => slot.Code == request.Code);
        if (codeExists)
        {
            return Conflict(new { message = "Parking slot code already exists." });
        }

        var slot = new ParkingSlot
        {
            Code = request.Code,
            Zone = request.Zone,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.ParkingSlots.Add(slot);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = slot.Id }, ToParkingSlotDto(slot));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParkingSlotDto>>> GetAll()
    {
        var slots = await _context.ParkingSlots
            .AsNoTracking()
            .OrderBy(slot => slot.Code)
            .Select(slot => ToParkingSlotDto(slot))
            .ToListAsync();

        return Ok(slots);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ParkingSlotDto>> GetById(int id)
    {
        var slot = await _context.ParkingSlots.AsNoTracking().FirstOrDefaultAsync(existingSlot => existingSlot.Id == id);
        return slot is null ? NotFound() : Ok(ToParkingSlotDto(slot));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ParkingSlotDto>> Update(int id, [FromBody] UpdateParkingSlotDto request)
    {
        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == id);
        if (slot is null)
        {
            return NotFound();
        }

        var codeExists = await _context.ParkingSlots.AnyAsync(existingSlot => existingSlot.Id != id && existingSlot.Code == request.Code);
        if (codeExists)
        {
            return Conflict(new { message = "Parking slot code already exists." });
        }

        slot.Code = request.Code;
        slot.Zone = request.Zone;
        slot.Status = request.Status;

        await _context.SaveChangesAsync();
        return Ok(ToParkingSlotDto(slot));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == id);
        if (slot is null)
        {
            return NotFound();
        }

        var hasReservations = await _context.ParkingReservations.AnyAsync(reservation => reservation.SlotId == id);
        if (hasReservations)
        {
            return Conflict(new { message = "Cannot delete parking slot with reservations." });
        }

        _context.ParkingSlots.Remove(slot);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ParkingSlotDto ToParkingSlotDto(ParkingSlot slot) => new()
    {
        Id = slot.Id,
        Code = slot.Code,
        Zone = slot.Zone,
        Status = slot.Status,
        CreatedAt = slot.CreatedAt
    };
}
