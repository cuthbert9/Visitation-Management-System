using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class ParkingReservationsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly ParkingReservationsController _controller;

    public ParkingReservationsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new ParkingReservationsController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<ParkingSlot?> ReloadSlotAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.ParkingSlots.FindAsync(id);
    }

    private async Task<ParkingReservation?> ReloadReservationAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.ParkingReservations.FindAsync(id);
    }

    [Fact]
    public async Task Create_VisitMissing_ReturnsBadRequest()
    {
        var slot = _context.AddParkingSlot();

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = 999, SlotId = slot.Id });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.ParkingReservations);
        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Create_SlotMissing_ReturnsBadRequest()
    {
        var visit = _context.AddVisit();

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = visit.Id, SlotId = 999 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.ParkingReservations);
    }

    [Fact]
    public async Task Create_SlotNotAvailable_ReturnsConflict()
    {
        var visit = _context.AddVisit();
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Occupied);

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = visit.Id, SlotId = slot.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Empty(_context.ParkingReservations);
        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Occupied, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Create_SlotAlreadyActivelyReserved_ReturnsConflict()
    {
        // A Pending reservation can coexist with an Available slot in practice (Update lets a
        // reservation move to Pending without touching the slot's own status), so this is the
        // realistic way to exercise the "active reservation on this slot" guard independently
        // from the "slot.Status != Available" guard above.
        var visit = _context.AddVisit();
        var otherVisit = _context.AddVisit();
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Available);
        _context.AddParkingReservation(visitId: otherVisit.Id, slotId: slot.Id, status: ParkingReservationStatus.Pending);

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = visit.Id, SlotId = slot.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, _context.ParkingReservations.Count());
    }

    [Fact]
    public async Task Create_VisitAlreadyHasActiveReservation_ReturnsConflict()
    {
        var visit = _context.AddVisit();
        var firstSlot = _context.AddParkingSlot(status: ParkingSlotStatus.Available);
        _context.AddParkingReservation(visitId: visit.Id, slotId: firstSlot.Id, status: ParkingReservationStatus.Reserved);
        var secondSlot = _context.AddParkingSlot(status: ParkingSlotStatus.Available);

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = visit.Id, SlotId = secondSlot.Id });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, _context.ParkingReservations.Count());
        var reloadedSecondSlot = await ReloadSlotAsync(secondSlot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSecondSlot!.Status);
    }

    [Fact]
    public async Task Create_Valid_ReservesSlotAndReturnsCreated()
    {
        var visit = _context.AddVisit();
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Available);

        var result = await _controller.Create(new CreateParkingReservationDto { VisitId = visit.Id, SlotId = slot.Id, VehiclePlate = "KAA-1" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(created.Value);
        Assert.Equal(ParkingReservationStatus.Reserved, dto.Status);
        Assert.Equal("KAA-1", dto.VehiclePlate);
        Assert.NotNull(dto.ReservedAt);
        Assert.Equal(1, _context.ParkingReservations.Count());

        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Reserved, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Release_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Release(999, new ReleaseParkingReservationDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Release_Valid_FreesSlotAndSetsReleasedAt()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Reserved);

        var result = await _controller.Release(reservation.Id, new ReleaseParkingReservationDto());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(ok.Value);
        Assert.Equal(ParkingReservationStatus.Released, dto.Status);
        Assert.NotNull(dto.ReleasedAt);

        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSlot!.Status);
        var reloadedReservation = await ReloadReservationAsync(reservation.Id);
        Assert.Equal(ParkingReservationStatus.Released, reloadedReservation!.Status);
    }

    [Fact]
    public async Task Release_SpecificReleasedAt_IsRespected()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Reserved);
        var explicitReleasedAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

        var result = await _controller.Release(reservation.Id, new ReleaseParkingReservationDto { ReleasedAt = explicitReleasedAt });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(ok.Value);
        Assert.Equal(explicitReleasedAt, dto.ReleasedAt);
    }

    [Fact]
    public async Task Cancel_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Cancel(999, new CancelParkingReservationDto());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Cancel_Valid_FreesSlotAndSetsStatus()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Reserved);

        var result = await _controller.Cancel(reservation.Id, new CancelParkingReservationDto());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(ok.Value);
        Assert.Equal(ParkingReservationStatus.Cancelled, dto.Status);
        Assert.NotNull(dto.ReleasedAt);

        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Update_SwitchToBusySlot_ReturnsConflict()
    {
        var currentSlot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: currentSlot.Id, status: ParkingReservationStatus.Reserved);
        var busySlot = _context.AddParkingSlot(status: ParkingSlotStatus.Occupied);

        var result = await _controller.Update(reservation.Id, new UpdateParkingReservationDto { SlotId = busySlot.Id, Status = ParkingReservationStatus.Reserved });

        Assert.IsType<ConflictObjectResult>(result.Result);

        var reloadedReservation = await ReloadReservationAsync(reservation.Id);
        Assert.Equal(currentSlot.Id, reloadedReservation!.SlotId);
        var reloadedCurrentSlot = await ReloadSlotAsync(currentSlot.Id);
        var reloadedBusySlot = await ReloadSlotAsync(busySlot.Id);
        Assert.Equal(ParkingSlotStatus.Reserved, reloadedCurrentSlot!.Status);
        Assert.Equal(ParkingSlotStatus.Occupied, reloadedBusySlot!.Status);
    }

    [Fact]
    public async Task Update_SwitchToAvailableSlot_FreesOldSlotAndReservesNew()
    {
        var currentSlot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: currentSlot.Id, status: ParkingReservationStatus.Reserved);
        var newSlot = _context.AddParkingSlot(status: ParkingSlotStatus.Available);

        var result = await _controller.Update(reservation.Id, new UpdateParkingReservationDto { SlotId = newSlot.Id, Status = ParkingReservationStatus.Reserved, VehiclePlate = "KBB-2" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(ok.Value);
        Assert.Equal(newSlot.Id, dto.SlotId);
        Assert.Equal("KBB-2", dto.VehiclePlate);

        var reloadedOldSlot = await ReloadSlotAsync(currentSlot.Id);
        var reloadedNewSlot = await ReloadSlotAsync(newSlot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedOldSlot!.Status);
        Assert.Equal(ParkingSlotStatus.Reserved, reloadedNewSlot!.Status);
    }

    [Fact]
    public async Task Update_StatusToReleased_FreesCurrentSlot()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Reserved);

        var result = await _controller.Update(reservation.Id, new UpdateParkingReservationDto { SlotId = slot.Id, Status = ParkingReservationStatus.Released });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingReservationDto>(ok.Value);
        Assert.Equal(ParkingReservationStatus.Released, dto.Status);
        Assert.NotNull(dto.ReleasedAt);

        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Delete_ReservedReservation_FreesSlot()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Reserved);

        var result = await _controller.Delete(reservation.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadReservationAsync(reservation.Id));
        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloadedSlot!.Status);
    }

    [Fact]
    public async Task Delete_NonReservedReservation_LeavesSlotStatusUntouched()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Occupied);
        var reservation = _context.AddParkingReservation(slotId: slot.Id, status: ParkingReservationStatus.Released);

        var result = await _controller.Delete(reservation.Id);

        Assert.IsType<NoContentResult>(result);
        var reloadedSlot = await ReloadSlotAsync(slot.Id);
        Assert.Equal(ParkingSlotStatus.Occupied, reloadedSlot!.Status);
    }
}
