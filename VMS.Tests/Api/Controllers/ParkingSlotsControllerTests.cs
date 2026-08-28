using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class ParkingSlotsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly ParkingSlotsController _controller;

    public ParkingSlotsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new ParkingSlotsController(_context);
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

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var existing = _context.AddParkingSlot(code: "P1", status: ParkingSlotStatus.Available);

        var result = await _controller.Create(new CreateParkingSlotDto { Code = "P1", Status = ParkingSlotStatus.Occupied });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, _context.ParkingSlots.Count());
        var reloaded = await ReloadSlotAsync(existing.Id);
        Assert.Equal(ParkingSlotStatus.Available, reloaded!.Status);
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreated()
    {
        var result = await _controller.Create(new CreateParkingSlotDto { Code = "P1", Zone = "North" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ParkingSlotDto>(created.Value);
        Assert.Equal("P1", dto.Code);
        Assert.Equal("North", dto.Zone);
        Assert.Equal(ParkingSlotStatus.Available, dto.Status);
        Assert.Equal(1, _context.ParkingSlots.Count());
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Update(999, new UpdateParkingSlotDto { Code = "P1" });

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_context.ParkingSlots);
    }

    [Fact]
    public async Task Update_DuplicateCode_ReturnsConflict()
    {
        _context.AddParkingSlot(code: "P1");
        var target = _context.AddParkingSlot(code: "P2", status: ParkingSlotStatus.Available);

        var result = await _controller.Update(target.Id, new UpdateParkingSlotDto { Code = "P1", Status = ParkingSlotStatus.Occupied });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadSlotAsync(target.Id);
        Assert.Equal("P2", reloaded!.Code);
        Assert.Equal(ParkingSlotStatus.Available, reloaded.Status);
    }

    [Fact]
    public async Task Update_Valid_UpdatesFields()
    {
        var slot = _context.AddParkingSlot(code: "P1");

        var result = await _controller.Update(slot.Id, new UpdateParkingSlotDto { Code = "P1", Zone = "South", Status = ParkingSlotStatus.Unavailable });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ParkingSlotDto>(ok.Value);
        Assert.Equal("South", dto.Zone);
        Assert.Equal(ParkingSlotStatus.Unavailable, dto.Status);

        var reloaded = await ReloadSlotAsync(slot.Id);
        Assert.Equal("South", reloaded!.Zone);
        Assert.Equal(ParkingSlotStatus.Unavailable, reloaded.Status);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_HasReservations_ReturnsConflict()
    {
        var slot = _context.AddParkingSlot(status: ParkingSlotStatus.Reserved);
        _context.AddParkingReservation(slotId: slot.Id);

        var result = await _controller.Delete(slot.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await ReloadSlotAsync(slot.Id));
    }

    [Fact]
    public async Task Delete_NoReservations_RemovesAndReturnsNoContent()
    {
        var slot = _context.AddParkingSlot();

        var result = await _controller.Delete(slot.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadSlotAsync(slot.Id));
    }
}
