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
        var visitorExists = await _context.Visitors.AnyAsync(visitor => visitor.Id == request.VisitorId);
        if (!visitorExists)
        {
            return BadRequest(new { message = "Visitor does not exist." });
        }

        var officeExists = await _context.Offices.AnyAsync(office => office.Id == request.OfficeId);
        if (!officeExists)
        {
            return BadRequest(new { message = "Office does not exist." });
        }

        if (request.CreatedById.HasValue)
        {
            var creatorExists = await _context.Users.AnyAsync(user => user.Id == request.CreatedById.Value);
            if (!creatorExists)
            {
                return BadRequest(new { message = "Creating user does not exist." });
            }
        }

        // var visitDate = request.VisitDate.Date;
        // var duplicateActiveVisit = await _context.Visits.AnyAsync(visit =>
        //     visit.VisitorId == request.VisitorId &&
        //     visit.VisitDate.Date == visitDate &&
        //     (visit.Status == VisitStatus.Pending ||
        //      visit.Status == VisitStatus.Approved ||
        //      visit.Status == VisitStatus.CheckedIn));

        // if (duplicateActiveVisit)
        // {
        //     return Conflict(new { message = "Visitor already has an active visit on this date." });
        // }

        var now = DateTime.UtcNow;
        var visit = new Visit
        {
            VisitorId = request.VisitorId,
            OfficeId = request.OfficeId,
            CreatedById = request.CreatedById,
            VisitDate = request.VisitDate,
            ExpectedArrival = request.ExpectedArrival,
            Purpose = request.Purpose,
            AttachmentUrl = request.AttachmentUrl,
            Status = VisitStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
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
            .Include(visit => visit.Visitor)
            .Include(visit => visit.Office)
            .Include(visit => visit.CheckIns)
            .Include(visit => visit.CheckOuts)
            .OrderByDescending(visit => visit.VisitDate)
            .Select(visit => VisitMappings.ToVisitDto(visit))
            .ToListAsync();

        return Ok(visits);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VisitDto>> GetById(int id)
    {
        var visit = await _context.Visits
            .AsNoTracking()
            .Include(visit => visit.Visitor)
            .Include(visit => visit.Office)
            .Include(visit => visit.CheckIns)
            .Include(visit => visit.CheckOuts)
            .FirstOrDefaultAsync(visit => visit.Id == id);

        return visit is null ? NotFound() : Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult<VisitDto>> Approve(int id, [FromBody] VisitApprovalDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        var approverExists = await _context.Users.AnyAsync(user => user.Id == request.ApprovedById);
        if (!approverExists)
        {
            return BadRequest(new { message = "Approving user does not exist." });
        }

        visit.Status = VisitStatus.Approved;
        visit.ApprovedById = request.ApprovedById;
        visit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/reject")]
    public async Task<ActionResult<VisitDto>> Reject(int id, [FromBody] VisitApprovalDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        var approverExists = await _context.Users.AnyAsync(user => user.Id == request.ApprovedById);
        if (!approverExists)
        {
            return BadRequest(new { message = "Rejecting user does not exist." });
        }

        visit.Status = VisitStatus.Rejected;
        visit.ApprovedById = request.ApprovedById;
        visit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/checkin")]
    public async Task<ActionResult<VisitDto>> CheckIn(int id, [FromBody] VisitCheckInDto request)
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

        var userExists = await _context.Users.AnyAsync(user => user.Id == request.CheckedInById);
        if (!userExists)
        {
            return BadRequest(new { message = "Check-in user does not exist." });
        }

        var now = DateTime.UtcNow;
        var checkIn = new VisitCheckIn
        {
            VisitId = visit.Id,
            CheckedInById = request.CheckedInById,
            CheckInTime = now,
            Gate = request.Gate,
            BadgeNumber = request.BadgeNumber,
            CreatedAt = now
        };

        visit.Status = VisitStatus.CheckedIn;
        visit.UpdatedAt = now;
        visit.CheckIns.Add(checkIn);

        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    [HttpPut("{id:int}/checkout")]
    public async Task<ActionResult<VisitDto>> CheckOut(int id, [FromBody] VisitCheckOutDto request)
    {
        var visit = await GetVisitEntity(id);
        if (visit is null)
        {
            return NotFound();
        }

        if (visit.Status != VisitStatus.CheckedIn)
        {
            return Conflict(new { message = "Visit must be checked in before checkout." });
        }

        var userExists = await _context.Users.AnyAsync(user => user.Id == request.CheckedOutById);
        if (!userExists)
        {
            return BadRequest(new { message = "Check-out user does not exist." });
        }

        var now = DateTime.UtcNow;
        var checkOut = new VisitCheckOut
        {
            VisitId = visit.Id,
            CheckedOutById = request.CheckedOutById,
            CheckOutTime = now,
            Remarks = request.Remarks,
            CreatedAt = now
        };

        visit.Status = VisitStatus.Completed;
        visit.UpdatedAt = now;
        visit.CheckOuts.Add(checkOut);

        await _context.SaveChangesAsync();
        return Ok(VisitMappings.ToVisitDto(visit));
    }

    private Task<Visit?> GetVisitEntity(int id)
    {
        return _context.Visits
            .Include(visit => visit.Visitor)
            .Include(visit => visit.Office)
            .Include(visit => visit.CheckIns)
            .Include(visit => visit.CheckOuts)
            .Include(visit => visit.ParkingReservations)
                .ThenInclude(reservation => reservation.Slot)
            .FirstOrDefaultAsync(visit => visit.Id == id);
    }
}

internal static class VisitMappings
{
    public static VisitDto ToVisitDto(Visit visit)
    {
        var latestCheckIn = visit.CheckIns
            .OrderByDescending(checkIn => checkIn.CheckInTime)
            .FirstOrDefault();

        var latestCheckOut = visit.CheckOuts
            .OrderByDescending(checkOut => checkOut.CheckOutTime)
            .FirstOrDefault();

        return new VisitDto
        {
            Id = visit.Id,
            VisitorId = visit.VisitorId,
            OfficeId = visit.OfficeId,
            CreatedById = visit.CreatedById,
            ApprovedById = visit.ApprovedById,
            VisitDate = visit.VisitDate,
            ExpectedArrival = visit.ExpectedArrival,
            Status = visit.Status,
            Purpose = visit.Purpose,
            AttachmentUrl = visit.AttachmentUrl,
            CreatedAt = visit.CreatedAt,
            UpdatedAt = visit.UpdatedAt,
            ParkingReservations = visit.ParkingReservations
                .OrderByDescending(reservation => reservation.Status == ParkingReservationStatus.Reserved)
                .ThenByDescending(reservation => reservation.ReservedAt ?? reservation.CreatedAt)
                .Select(reservation => new VisitParkingReservationDto
                {
                    Id = reservation.Id,
                    SlotId = reservation.SlotId,
                    SlotCode = reservation.Slot.Code,
                    Zone = reservation.Slot.Zone,
                    VehiclePlate = reservation.VehiclePlate,
                    Status = reservation.Status,
                    ReservedAt = reservation.ReservedAt,
                    ReleasedAt = reservation.ReleasedAt
                })
                .ToList(),
            LatestCheckIn = latestCheckIn is null
                ? null
                : new VisitCheckInRecordDto
                {
                    Id = latestCheckIn.Id,
                    VisitId = latestCheckIn.VisitId,
                    CheckedInById = latestCheckIn.CheckedInById,
                    CheckInTime = latestCheckIn.CheckInTime,
                    Gate = latestCheckIn.Gate,
                    BadgeNumber = latestCheckIn.BadgeNumber,
                    CreatedAt = latestCheckIn.CreatedAt
                },
            LatestCheckOut = latestCheckOut is null
                ? null
                : new VisitCheckOutRecordDto
                {
                    Id = latestCheckOut.Id,
                    VisitId = latestCheckOut.VisitId,
                    CheckedOutById = latestCheckOut.CheckedOutById,
                    CheckOutTime = latestCheckOut.CheckOutTime,
                    Remarks = latestCheckOut.Remarks,
                    CreatedAt = latestCheckOut.CreatedAt
                },
            Visitor = new VisitorDto
            {
                Id = visit.Visitor.Id,
                FullName = visit.Visitor.FullName,
                Phone = visit.Visitor.Phone,
                NationalId = visit.Visitor.NationalId,
                Company = visit.Visitor.Company,
                VehiclePlate = visit.Visitor.VehiclePlate,
                PhotoUrl = visit.Visitor.PhotoUrl,
                CreatedAt = visit.Visitor.CreatedAt,
                UpdatedAt = visit.Visitor.UpdatedAt
            },
            Office = new OfficeDto
            {
                Id = visit.Office.Id,
                Name = visit.Office.Name,
                Floor = visit.Office.Floor,
                OfficeCode = visit.Office.OfficeCode,
                CreatedAt = visit.Office.CreatedAt,
                UpdatedAt = visit.Office.UpdatedAt
            }
        };
    }
}
