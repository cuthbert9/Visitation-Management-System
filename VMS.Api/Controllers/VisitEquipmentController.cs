using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitEquipmentController : ControllerBase
{
    private readonly AppDbContext _context;

    public VisitEquipmentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<VisitEquipmentDto>> Create([FromBody] CreateVisitEquipmentDto request)
    {
        var visitExists = await _context.Visits.AnyAsync(visit => visit.Id == request.VisitId);
        if (!visitExists)
        {
            return BadRequest(new { message = "Visit does not exist." });
        }

        var now = DateTime.UtcNow;
        var equipment = await _context.VisitEquipment.FirstOrDefaultAsync(existing => existing.VisitId == request.VisitId);

        if (equipment is null)
        {
            equipment = new VisitEquipment
            {
                VisitId = request.VisitId,
                HasLaptop = request.HasLaptop,
                DeviceType = request.HasLaptop ? request.DeviceType : null,
                DeviceBrand = request.HasLaptop ? request.DeviceBrand : null,
                AssetTag = request.HasLaptop ? request.AssetTag : null,
                PcConfirmedReturned = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.VisitEquipment.Add(equipment);
        }
        else
        {
            equipment.HasLaptop = request.HasLaptop;
            equipment.DeviceType = request.HasLaptop ? request.DeviceType : null;
            equipment.DeviceBrand = request.HasLaptop ? request.DeviceBrand : null;
            equipment.AssetTag = request.HasLaptop ? request.AssetTag : null;
            equipment.UpdatedAt = now;
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByVisit), new { visitId = equipment.VisitId }, ToVisitEquipmentDto(equipment));
    }

    [HttpGet("visit/{visitId:int}")]
    public async Task<ActionResult<VisitEquipmentDto>> GetByVisit(int visitId)
    {
        var equipment = await _context.VisitEquipment
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.VisitId == visitId);

        return equipment is null ? NotFound() : Ok(ToVisitEquipmentDto(equipment));
    }

    [HttpPatch("visit/{visitId:int}/confirm-returned")]
    public async Task<ActionResult<VisitEquipmentDto>> ConfirmReturned(int visitId)
    {
        var equipment = await _context.VisitEquipment.FirstOrDefaultAsync(existing => existing.VisitId == visitId);
        if (equipment is null)
        {
            return NotFound();
        }

        equipment.PcConfirmedReturned = true;
        equipment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToVisitEquipmentDto(equipment));
    }

    private static VisitEquipmentDto ToVisitEquipmentDto(VisitEquipment equipment) => new()
    {
        Id = equipment.Id,
        VisitId = equipment.VisitId,
        HasLaptop = equipment.HasLaptop,
        DeviceType = equipment.DeviceType,
        DeviceBrand = equipment.DeviceBrand,
        AssetTag = equipment.AssetTag,
        PcConfirmedReturned = equipment.PcConfirmedReturned
    };
}
