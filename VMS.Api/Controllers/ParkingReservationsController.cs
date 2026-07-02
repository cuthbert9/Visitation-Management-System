using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParkingReservationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ParkingReservationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ParkingReservationDto>> Create([FromBody] CreateParkingReservationDto request)
    {
        var visit = await _context.Visits.FirstOrDefaultAsync(existingVisit => existingVisit.Id == request.VisitId);
        if (visit is null)
        {
            return BadRequest(new { message = "Visit does not exist." });
        }

        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == request.SlotId);
        if (slot is null)
        {
            return BadRequest(new { message = "Parking slot does not exist." });
        }

        if (slot.Status != ParkingSlotStatus.Available)
        {
            return Conflict(new { message = "Parking slot is not available." });
        }

        var activeReservationForSlot = await _context.ParkingReservations.AnyAsync(reservation =>
            reservation.SlotId == request.SlotId &&
            (reservation.Status == ParkingReservationStatus.Pending || reservation.Status == ParkingReservationStatus.Reserved));

        if (activeReservationForSlot)
        {
            return Conflict(new { message = "Parking slot already has an active reservation." });
        }

        var activeReservationForVisit = await _context.ParkingReservations.AnyAsync(reservation =>
            reservation.VisitId == request.VisitId &&
            (reservation.Status == ParkingReservationStatus.Pending || reservation.Status == ParkingReservationStatus.Reserved));

        if (activeReservationForVisit)
        {
            return Conflict(new { message = "Visit already has an active parking reservation." });
        }

        var now = DateTime.UtcNow;
        var reservation = new ParkingReservation
        {
            VisitId = request.VisitId,
            SlotId = request.SlotId,
            VehiclePlate = request.VehiclePlate,
            Status = ParkingReservationStatus.Reserved,
            ReservedAt = now,
            CreatedAt = now
        };

        slot.Status = ParkingSlotStatus.Reserved;
        _context.ParkingReservations.Add(reservation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, ToParkingReservationDto(reservation));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParkingReservationDto>>> GetAll()
    {
        var reservations = await _context.ParkingReservations
            .AsNoTracking()
            .OrderByDescending(reservation => reservation.CreatedAt)
            .Select(reservation => ToParkingReservationDto(reservation))
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ParkingReservationDto>> GetById(int id)
    {
        var reservation = await _context.ParkingReservations
            .AsNoTracking()
            .FirstOrDefaultAsync(existingReservation => existingReservation.Id == id);

        return reservation is null ? NotFound() : Ok(ToParkingReservationDto(reservation));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ParkingReservationDto>> Update(int id, [FromBody] UpdateParkingReservationDto request)
    {
        var reservation = await _context.ParkingReservations.FirstOrDefaultAsync(existingReservation => existingReservation.Id == id);
        if (reservation is null)
        {
            return NotFound();
        }

        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == request.SlotId);
        if (slot is null)
        {
            return BadRequest(new { message = "Parking slot does not exist." });
        }

        if (reservation.SlotId != request.SlotId)
        {
            var targetSlotBusy = await _context.ParkingReservations.AnyAsync(existingReservation =>
                existingReservation.Id != id &&
                existingReservation.SlotId == request.SlotId &&
                (existingReservation.Status == ParkingReservationStatus.Pending || existingReservation.Status == ParkingReservationStatus.Reserved));

            if (targetSlotBusy || slot.Status != ParkingSlotStatus.Available)
            {
                return Conflict(new { message = "Target parking slot is not available." });
            }

            var currentSlot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == reservation.SlotId);
            if (currentSlot is not null && reservation.Status == ParkingReservationStatus.Reserved)
            {
                currentSlot.Status = ParkingSlotStatus.Available;
            }

            slot.Status = request.Status == ParkingReservationStatus.Reserved
                ? ParkingSlotStatus.Reserved
                : slot.Status;

            reservation.SlotId = request.SlotId;
        }

        reservation.VehiclePlate = request.VehiclePlate;
        reservation.Status = request.Status;

        if (reservation.Status == ParkingReservationStatus.Reserved && reservation.ReservedAt is null)
        {
            reservation.ReservedAt = DateTime.UtcNow;
        }

        if (reservation.Status is ParkingReservationStatus.Released or ParkingReservationStatus.Cancelled)
        {
            reservation.ReleasedAt = DateTime.UtcNow;
            slot.Status = ParkingSlotStatus.Available;
        }

        await _context.SaveChangesAsync();
        return Ok(ToParkingReservationDto(reservation));
    }

    [HttpPut("{id:int}/release")]
    public async Task<ActionResult<ParkingReservationDto>> Release(int id, [FromBody] ReleaseParkingReservationDto request)
    {
        var reservation = await _context.ParkingReservations.FirstOrDefaultAsync(existingReservation => existingReservation.Id == id);
        if (reservation is null)
        {
            return NotFound();
        }

        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == reservation.SlotId);
        if (slot is null)
        {
            return BadRequest(new { message = "Parking slot does not exist." });
        }

        reservation.Status = ParkingReservationStatus.Released;
        reservation.ReleasedAt = request.ReleasedAt ?? DateTime.UtcNow;
        slot.Status = ParkingSlotStatus.Available;

        await _context.SaveChangesAsync();
        return Ok(ToParkingReservationDto(reservation));
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<ParkingReservationDto>> Cancel(int id, [FromBody] CancelParkingReservationDto request)
    {
        var reservation = await _context.ParkingReservations.FirstOrDefaultAsync(existingReservation => existingReservation.Id == id);
        if (reservation is null)
        {
            return NotFound();
        }

        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == reservation.SlotId);
        if (slot is null)
        {
            return BadRequest(new { message = "Parking slot does not exist." });
        }

        reservation.Status = ParkingReservationStatus.Cancelled;
        reservation.ReleasedAt = DateTime.UtcNow;
        slot.Status = ParkingSlotStatus.Available;

        await _context.SaveChangesAsync();
        return Ok(ToParkingReservationDto(reservation));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _context.ParkingReservations.FirstOrDefaultAsync(existingReservation => existingReservation.Id == id);
        if (reservation is null)
        {
            return NotFound();
        }

        var slot = await _context.ParkingSlots.FirstOrDefaultAsync(existingSlot => existingSlot.Id == reservation.SlotId);
        if (slot is not null && reservation.Status == ParkingReservationStatus.Reserved)
        {
            slot.Status = ParkingSlotStatus.Available;
        }

        _context.ParkingReservations.Remove(reservation);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ParkingReservationDto ToParkingReservationDto(ParkingReservation reservation) => new()
    {
        Id = reservation.Id,
        VisitId = reservation.VisitId,
        SlotId = reservation.SlotId,
        VehiclePlate = reservation.VehiclePlate,
        Status = reservation.Status,
        ReservedAt = reservation.ReservedAt,
        ReleasedAt = reservation.ReleasedAt,
        CreatedAt = reservation.CreatedAt
    };
}
