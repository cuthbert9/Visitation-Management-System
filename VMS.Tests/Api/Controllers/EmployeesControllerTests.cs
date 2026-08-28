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

public class EmployeesControllerTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _factory = new();
    private readonly AppDbContext _context;
    private readonly Mock<IAuditLogService> _auditLog = new();
    private readonly EmployeesController _controller;

    public EmployeesControllerTests()
    {
        _context = _factory.CreateContext();
        _controller = new EmployeesController(_context, _auditLog.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _factory.Dispose();
    }

    private async Task<Employee?> ReloadEmployeeAsync(int id)
    {
        await using var verifyContext = _factory.CreateContext();
        return await verifyContext.Employees.FindAsync(id);
    }

    private void VerifyAuditLogNeverCalled() => _auditLog.Verify(log => log.LogAsync(
        It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(),
        It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()),
        Times.Never);

    [Fact]
    public async Task Create_DepartmentMissing_ReturnsBadRequest()
    {
        var request = new CreateEmployeeDto { EmployeeNumber = "E1", FullName = "Jane", DepartmentId = 999 };

        var result = await _controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(_context.Employees);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Create_DuplicateEmployeeNumber_ReturnsConflict()
    {
        var department = _context.AddDepartment();
        var existing = _context.AddEmployee(departmentId: department.Id, employeeNumber: "E1", fullName: "Existing");

        var request = new CreateEmployeeDto { EmployeeNumber = "E1", FullName = "Jane", DepartmentId = department.Id };
        var result = await _controller.Create(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, _context.Employees.Count());
        var reloaded = await ReloadEmployeeAsync(existing.Id);
        Assert.Equal("Existing", reloaded!.FullName);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreatedAndPersists()
    {
        var department = _context.AddDepartment();
        var request = new CreateEmployeeDto { EmployeeNumber = "E1", FullName = "Jane", DepartmentId = department.Id, Position = "Manager" };

        var result = await _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<EmployeeDto>(created.Value);
        Assert.Equal("Jane", dto.FullName);
        Assert.Equal("Manager", dto.Position);
        Assert.True(dto.IsActive);
        Assert.Equal(1, _context.Employees.Count());
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Create, nameof(Employee), dto.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNotFound()
    {
        var result = await _controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByDepartment_DepartmentMissing_ReturnsNotFound()
    {
        var result = await _controller.GetByDepartment(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByDepartment_ReturnsOnlyMatchingEmployees()
    {
        var departmentA = _context.AddDepartment(code: "A");
        var departmentB = _context.AddDepartment(code: "B");
        _context.AddEmployee(departmentId: departmentA.Id, employeeNumber: "E1", fullName: "Alice");
        _context.AddEmployee(departmentId: departmentB.Id, employeeNumber: "E2", fullName: "Bob");

        var result = await _controller.GetByDepartment(departmentA.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var employees = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(ok.Value).ToList();
        var employee = Assert.Single(employees);
        Assert.Equal("Alice", employee.FullName);
        Assert.Equal(departmentA.Id, employee.DepartmentId);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var department = _context.AddDepartment();
        var request = new UpdateEmployeeDto { EmployeeNumber = "E1", FullName = "Jane", DepartmentId = department.Id };

        var result = await _controller.Update(999, request);

        Assert.IsType<NotFoundResult>(result.Result);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_DepartmentMissing_ReturnsBadRequest()
    {
        var department = _context.AddDepartment();
        var employee = _context.AddEmployee(departmentId: department.Id, employeeNumber: "E1", fullName: "Jane");
        var request = new UpdateEmployeeDto { EmployeeNumber = "E1", FullName = "Renamed", DepartmentId = 999 };

        var result = await _controller.Update(employee.Id, request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var reloaded = await ReloadEmployeeAsync(employee.Id);
        Assert.Equal("Jane", reloaded!.FullName);
        Assert.Equal(department.Id, reloaded.DepartmentId);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_DuplicateEmployeeNumberOnAnother_ReturnsConflict()
    {
        var department = _context.AddDepartment();
        _context.AddEmployee(departmentId: department.Id, employeeNumber: "E1");
        var target = _context.AddEmployee(departmentId: department.Id, employeeNumber: "E2", fullName: "Target");

        var request = new UpdateEmployeeDto { EmployeeNumber = "E1", FullName = "Renamed", DepartmentId = department.Id };
        var result = await _controller.Update(target.Id, request);

        Assert.IsType<ConflictObjectResult>(result.Result);
        var reloaded = await ReloadEmployeeAsync(target.Id);
        Assert.Equal("E2", reloaded!.EmployeeNumber);
        Assert.Equal("Target", reloaded.FullName);
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Update_SameNumberAsSelf_Succeeds()
    {
        var department = _context.AddDepartment();
        var employee = _context.AddEmployee(departmentId: department.Id, employeeNumber: "E1", fullName: "Jane");

        var request = new UpdateEmployeeDto { EmployeeNumber = "E1", FullName = "Jane Renamed", DepartmentId = department.Id, IsActive = false };
        var result = await _controller.Update(employee.Id, request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<EmployeeDto>(ok.Value);
        Assert.Equal("Jane Renamed", dto.FullName);
        Assert.False(dto.IsActive);

        var reloaded = await ReloadEmployeeAsync(employee.Id);
        Assert.Equal("Jane Renamed", reloaded!.FullName);
        Assert.False(reloaded.IsActive);
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Update, nameof(Employee), employee.Id, null,
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
        var department = _context.AddDepartment();
        var employee = _context.AddEmployee(departmentId: department.Id);
        _context.AddVisit(hostEmployeeId: employee.Id);

        var result = await _controller.Delete(employee.Id);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(await ReloadEmployeeAsync(employee.Id));
        VerifyAuditLogNeverCalled();
    }

    [Fact]
    public async Task Delete_NoVisits_RemovesAndReturnsNoContent()
    {
        var department = _context.AddDepartment();
        var employee = _context.AddEmployee(departmentId: department.Id);

        var result = await _controller.Delete(employee.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await ReloadEmployeeAsync(employee.Id));
        _auditLog.Verify(log => log.LogAsync(
            AuditAction.Delete, nameof(Employee), employee.Id, null,
            It.IsAny<string?>(), null, null, ActorType.User, default), Times.Once);
    }
}
