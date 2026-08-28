using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class VisitItemsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly VisitItemsController _controller;

    public VisitItemsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new VisitItemsController(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<VisitItem?> ReloadItemAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.VisitItems.FindAsync(id);
    }

    [Fact]
    public async Task Create_VisitMissing_ReturnsBadRequest()
    {
        var result = await _controller.Create(new CreateVisitItemDto { VisitId = 999, ItemName = "Laptop" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.VisitItems);
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreated()
    {
        var visit = _context.AddVisit();

        var result = await _controller.Create(new CreateVisitItemDto { VisitId = visit.Id, ItemName = "Laptop", Quantity = 2 });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitItemDto>(created.Value);
        Assert.Equal("Laptop", dto.ItemName);
        Assert.Equal(2, dto.Quantity);
        Assert.Equal(visit.Id, dto.VisitId);
        Assert.Equal(1, _context.VisitItems.Count());
    }

    [Fact]
    public async Task GetByVisit_VisitMissing_ReturnsNotFound()
    {
        var result = await _controller.GetByVisit(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByVisit_ReturnsOnlyMatchingItems()
    {
        var visitA = _context.AddVisit();
        var visitB = _context.AddVisit();
        await _controller.Create(new CreateVisitItemDto { VisitId = visitA.Id, ItemName = "Laptop" });
        await _controller.Create(new CreateVisitItemDto { VisitId = visitB.Id, ItemName = "Phone" });

        var result = await _controller.GetByVisit(visitA.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<VisitItemDto>>(ok.Value).ToList();
        var item = Assert.Single(items);
        Assert.Equal("Laptop", item.ItemName);
        Assert.Equal(visitA.Id, item.VisitId);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Update(999, new UpdateVisitItemDto { ItemName = "X" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_Valid_UpdatesFields()
    {
        var visit = _context.AddVisit();
        var createResult = await _controller.Create(new CreateVisitItemDto { VisitId = visit.Id, ItemName = "Laptop", Quantity = 1 });
        var createdDto = Assert.IsType<VisitItemDto>(((CreatedAtActionResult)createResult.Result!).Value);

        var result = await _controller.Update(createdDto.Id, new UpdateVisitItemDto { ItemName = "Laptop Pro", Quantity = 3, SerialNumber = "SN-1" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitItemDto>(ok.Value);
        Assert.Equal("Laptop Pro", dto.ItemName);
        Assert.Equal(3, dto.Quantity);

        var reloaded = await ReloadItemAsync(createdDto.Id);
        Assert.Equal("Laptop Pro", reloaded!.ItemName);
        Assert.Equal("SN-1", reloaded.SerialNumber);
        Assert.Equal(visit.Id, reloaded.VisitId);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Valid_RemovesItem()
    {
        var visit = _context.AddVisit();
        var createResult = await _controller.Create(new CreateVisitItemDto { VisitId = visit.Id, ItemName = "Laptop" });
        var createdDto = Assert.IsType<VisitItemDto>(((CreatedAtActionResult)createResult.Result!).Value);

        var result = await _controller.Delete(createdDto.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadItemAsync(createdDto.Id));
    }
}
