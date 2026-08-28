using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class VisitorsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly VisitorsController _controller;

    public VisitorsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new VisitorsController(_context, _auditLog.Object, NullLogger<VisitorsController>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<Visitor?> ReloadVisitorAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.Visitors.FindAsync(id);
    }

    private void VerifyAuditLogNeverCalled() => _auditLog.Verify(log => log.LogAsync(
        It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()),
        Times.Never);

    [Fact]
    public async Task Create_Valid_ReturnsCreatedAndCallsAuditLog()
    {
        var request = new CreateVisitorDto
        {
            FullName = "Alice Visitor",
            PhoneNumber = "0700000000",
            IdentificationType = IdentificationType.NationalId,
            IdentificationNumber = "ID-123"
        };

        var result = await _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<VisitorDto>(created.Value);
        Assert.Equal("Alice Visitor", dto.FullName);
        Assert.True(dto.IsActive);
        Assert.Equal(1, _context.Visitors.Count());
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Create, nameof(Visitor), dto.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var visitor = _context.AddVisitor();

        var result = await _controller.GetById(visitor.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitorDto>(ok.Value);
        Assert.Equal(visitor.Id, dto.Id);
        Assert.Equal(visitor.FullName, dto.FullName);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetVisitorVisits_VisitorMissing_ReturnsNotFound()
    {
        var result = await _controller.GetVisitorVisits(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetVisitorVisits_ReturnsOnlyThatVisitorsVisits()
    {
        var visitorA = _context.AddVisitor(fullName: "A");
        var visitorB = _context.AddVisitor(fullName: "B");
        var visitA = _context.AddVisit(visitorId: visitorA.Id);
        _context.AddVisit(visitorId: visitorB.Id);

        var result = await _controller.GetVisitorVisits(visitorA.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var visits = Assert.IsAssignableFrom<IEnumerable<VisitDto>>(ok.Value).ToList();
        var visit = Assert.Single(visits);
        Assert.Equal(visitA.Id, visit.Id);
        Assert.Equal(visitorA.Id, visit.VisitorId);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var request = new UpdateVisitorDto
        {
            FullName = "X",
            PhoneNumber = "0700000000",
            IdentificationType = IdentificationType.NationalId,
            IdentificationNumber = "ID-1"
        };

        var result = await _controller.Update(999, request);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_context.Visitors);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_Valid_UpdatesFields()
    {
        var visitor = _context.AddVisitor(fullName: "Old Name");
        var request = new UpdateVisitorDto
        {
            FullName = "New Name",
            PhoneNumber = "0711111111",
            IdentificationType = IdentificationType.Passport,
            IdentificationNumber = "P-999",
            IsActive = false
        };

        var result = await _controller.Update(visitor.Id, request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<VisitorDto>(ok.Value);
        Assert.Equal("New Name", dto.FullName);
        Assert.Equal(IdentificationType.Passport, dto.IdentificationType);
        Assert.False(dto.IsActive);

        var reloaded = await ReloadVisitorAsync(visitor.Id);
        Assert.Equal("New Name", reloaded!.FullName);
        Assert.Equal("P-999", reloaded.IdentificationNumber);
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Update, nameof(Visitor), visitor.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Delete_HasVisits_ReturnsConflict()
    {
        var visitor = _context.AddVisitor();
        _context.AddVisit(visitorId: visitor.Id);

        var result = await _controller.Delete(visitor.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await ReloadVisitorAsync(visitor.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Delete_NoVisits_RemovesAndReturnsNoContent()
    {
        var visitor = _context.AddVisitor();

        var result = await _controller.Delete(visitor.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadVisitorAsync(visitor.Id));
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Delete, nameof(Visitor), visitor.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }
}
