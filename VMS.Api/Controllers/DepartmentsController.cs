using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Api.Services;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Domain.Enums;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public DepartmentsController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentDto request)
    {
        var codeExists = await _context.Departments.AnyAsync(department => department.Code == request.Code);
        if (codeExists)
        {
            return Conflict(new { message = "Department code already exists." });
        }

        var now = DateTime.UtcNow;
        var department = new Department
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Create, nameof(Department), department.Id, null, "Department created.");

        return CreatedAtAction(nameof(GetById), new { id = department.Id }, ToDepartmentDto(department));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderByDescending(department => department.Id)
            .Select(department => ToDepartmentDto(department))
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(existingDepartment => existingDepartment.Id == id);

        return department is null ? NotFound() : Ok(ToDepartmentDto(department));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] UpdateDepartmentDto request)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(existingDepartment => existingDepartment.Id == id);
        if (department is null)
        {
            return NotFound();
        }

        var codeExists = await _context.Departments.AnyAsync(existingDepartment =>
            existingDepartment.Id != id && existingDepartment.Code == request.Code);
        if (codeExists)
        {
            return Conflict(new { message = "Department code already exists." });
        }

        department.Code = request.Code;
        department.Name = request.Name;
        department.Description = request.Description;
        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Update, nameof(Department), department.Id, null, "Department updated.");

        return Ok(ToDepartmentDto(department));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(existingDepartment => existingDepartment.Id == id);
        if (department is null)
        {
            return NotFound();
        }

        var hasEmployees = await _context.Employees.AnyAsync(employee => employee.DepartmentId == id);
        var hasVisits = await _context.Visits.AnyAsync(visit => visit.DepartmentId == id);
        if (hasEmployees || hasVisits)
        {
            return Conflict(new { message = "Cannot delete department with existing employees or visits." });
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Delete, nameof(Department), id, null, "Department deleted.");

        return NoContent();
    }

    private static DepartmentDto ToDepartmentDto(Department department) => new()
    {
        Id = department.Id,
        Code = department.Code,
        Name = department.Name,
        Description = department.Description,
        IsActive = department.IsActive,
        CreatedAt = department.CreatedAt,
        UpdatedAt = department.UpdatedAt
    };
}
