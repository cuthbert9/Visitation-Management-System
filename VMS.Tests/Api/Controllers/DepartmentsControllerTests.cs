using Microsoft.AspNetCore.Mvc;
using Moq;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;
using VMS.Tests.Common;

namespace VMS.Tests.Api.Controllers;

public class DepartmentsControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly DepartmentsController _controller;

    public DepartmentsControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new DepartmentsController(_context, _auditLog.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<Department?> ReloadDepartmentAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.Departments.FindAsync(id);
    }

    private void VerifyAuditLogNeverCalled() => _auditLog.Verify(log => log.LogAsync(
        It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()),
        Times.Never);

    [Fact]
    public async Task Create_NewCode_ReturnsCreatedAndPersists()
    {
        var request = new CreateDepartmentDto { Code = "ENG", Name = "Engineering" };

        var result = await _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<DepartmentDto>(created.Value);
        Assert.Equal("ENG", dto.Code);
        Assert.True(dto.IsActive);
        Assert.Equal(1, _context.Departments.Count());
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Create, nameof(Department), dto.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateCode_ReturnsConflict()
    {
        var existing = _context.AddDepartment(code: "ENG", name: "Engineering");

        var result = await _controller.Create(new CreateDepartmentDto { Code = "ENG", Name = "Engineering 2" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, _context.Departments.Count());
        var reloaded = await ReloadDepartmentAsync(existing.Id);
        Assert.Equal("Engineering", reloaded!.Name);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var department = _context.AddDepartment();

        var result = await _controller.GetById(department.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DepartmentDto>(ok.Value);
        Assert.Equal(department.Id, dto.Id);
        Assert.Equal(department.Code, dto.Code);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var result = await _controller.Update(999, new UpdateDepartmentDto { Code = "X", Name = "X" });

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Empty(_context.Departments);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_DuplicateCodeOnAnotherDepartment_ReturnsConflict()
    {
        _context.AddDepartment(code: "HR");
        var target = _context.AddDepartment(code: "FIN", name: "Finance");

        var result = await _controller.Update(target.Id, new UpdateDepartmentDto { Code = "HR", Name = "Finance Renamed" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadDepartmentAsync(target.Id);
        Assert.Equal("FIN", reloaded!.Code);
        Assert.Equal("Finance", reloaded.Name);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_SameCodeAsSelf_Succeeds()
    {
        var department = _context.AddDepartment(code: "HR", name: "Human Resources");

        var result = await _controller.Update(department.Id, new UpdateDepartmentDto { Code = "HR", Name = "HR Renamed", IsActive = false });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DepartmentDto>(ok.Value);
        Assert.Equal("HR Renamed", dto.Name);
        Assert.False(dto.IsActive);

        var reloaded = await ReloadDepartmentAsync(department.Id);
        Assert.Equal("HR Renamed", reloaded!.Name);
        Assert.False(reloaded.IsActive);
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Update, nameof(Department), department.Id, null,
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
    public async Task Delete_HasEmployees_ReturnsConflict()
    {
        var department = _context.AddDepartment();
        _context.AddEmployee(departmentId: department.Id);

        var result = await _controller.Delete(department.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await ReloadDepartmentAsync(department.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Delete_HasVisits_ReturnsConflict()
    {
        var department = _context.AddDepartment();
        _context.AddVisit(departmentId: department.Id);

        var result = await _controller.Delete(department.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await ReloadDepartmentAsync(department.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Delete_NoDependents_RemovesAndReturnsNoContent()
    {
        var department = _context.AddDepartment();

        var result = await _controller.Delete(department.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadDepartmentAsync(department.Id));
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Delete, nameof(Department), department.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }
}
