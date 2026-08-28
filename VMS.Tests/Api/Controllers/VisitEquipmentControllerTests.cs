using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class VisitEquipmentControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly VisitEquipmentController _controller;

    public VisitEquipmentControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new VisitEquipmentController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Create_VisitMissing_ReturnsBadRequest()
    {
        var result = await _controller.Create(new CreateVisitEquipmentDto { VisitId = 999, HasLaptop = true });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.VisitEquipment);
    }

    [Fact]
    public async Task Create_FirstTime_CreatesEquipment()
    {
        var visit = _context.AddVisit();

        var result = await _controller.Create(new CreateVisitEquipmentDto
        {
            VisitId = visit.Id,
            HasLaptop = true,
            DeviceType = "Laptop",
            DeviceBrand = "Dell",
            AssetTag = "AT-1"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitEquipmentDto>(created.Value);
        Assert.True(dto.HasLaptop);
        Assert.Equal("Dell", dto.DeviceBrand);
        Assert.Equal("AT-1", dto.AssetTag);
        Assert.False(dto.PcConfirmedReturned);
        Assert.Equal(1, _context.VisitEquipment.Count());
    }

    [Fact]
    public async Task Create_SecondTime_UpdatesExistingRowInPlaceInsteadOfDuplicating()
    {
        var visit = _context.AddVisit();
        var firstResult = await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = true, DeviceBrand = "Dell" });
        var firstDto = Assert.IsType<VisitEquipmentDto>(((CreatedAtActionResult)firstResult.Result!).Value);

        var secondResult = await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = true, DeviceBrand = "HP" });

        var created = Assert.IsType<CreatedAtActionResult>(secondResult.Result);
        var secondDto = Assert.IsType<VisitEquipmentDto>(created.Value);
        Assert.Equal(firstDto.Id, secondDto.Id);
        Assert.Equal("HP", secondDto.DeviceBrand);
        Assert.Equal(1, _context.VisitEquipment.Count());
    }

    [Fact]
    public async Task Create_HasLaptopFalse_ClearsDeviceFields()
    {
        var visit = _context.AddVisit();

        var result = await _controller.Create(new CreateVisitEquipmentDto
        {
            VisitId = visit.Id,
            HasLaptop = false,
            DeviceType = "Laptop",
            DeviceBrand = "Dell",
            AssetTag = "AT-1"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitEquipmentDto>(created.Value);
        Assert.False(dto.HasLaptop);
        Assert.Null(dto.DeviceType);
        Assert.Null(dto.DeviceBrand);
        Assert.Null(dto.AssetTag);
    }

    [Fact]
    public async Task Create_SecondTimeHasLaptopFalse_ClearsPreviouslySetDeviceFields()
    {
        var visit = _context.AddVisit();
        await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = true, DeviceBrand = "Dell", AssetTag = "AT-1" });

        var result = await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = false, DeviceBrand = "Dell", AssetTag = "AT-1" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitEquipmentDto>(created.Value);
        Assert.False(dto.HasLaptop);
        Assert.Null(dto.DeviceBrand);
        Assert.Null(dto.AssetTag);
    }

    [Fact]
    public async Task GetByVisit_Missing_ReturnsNotFound()
    {
        var result = await _controller.GetByVisit(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByVisit_Existing_ReturnsIt()
    {
        var visit = _context.AddVisit();
        await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = true, DeviceBrand = "Dell" });

        var result = await _controller.GetByVisit(visit.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitEquipmentDto>(ok.Value);
        Assert.Equal(visit.Id, dto.VisitId);
        Assert.Equal("Dell", dto.DeviceBrand);
    }

    [Fact]
    public async Task ConfirmReturned_NotFound_ReturnsNotFound()
    {
        var result = await _controller.ConfirmReturned(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ConfirmReturned_Valid_SetsFlag()
    {
        var visit = _context.AddVisit();
        await _controller.Create(new CreateVisitEquipmentDto { VisitId = visit.Id, HasLaptop = true });

        var result = await _controller.ConfirmReturned(visit.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitEquipmentDto>(ok.Value);
        Assert.True(dto.PcConfirmedReturned);
    }
}
