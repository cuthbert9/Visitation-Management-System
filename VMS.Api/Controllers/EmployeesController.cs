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
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public EmployeesController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeDto request)
    {
        var departmentExists = await _context.Departments.AnyAsync(department => department.Id == request.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        var employeeNumberExists = await _context.Employees.AnyAsync(employee => employee.EmployeeNumber == request.EmployeeNumber);
        if (employeeNumberExists)
        {
            return Conflict(new { message = "Employee number already exists." });
        }

        var now = DateTime.UtcNow;
        var employee = new Employee
        {
            EmployeeNumber = request.EmployeeNumber,
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Position = request.Position,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Create, nameof(Employee), employee.Id, null, "Employee created.");

        var created = await GetEmployeeEntity(employee.Id);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, ToEmployeeDto(created!));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Department)
            .OrderBy(employee => employee.FullName)
            .Select(employee => ToEmployeeDto(employee))
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(existingEmployee => existingEmployee.Department)
            .FirstOrDefaultAsync(existingEmployee => existingEmployee.Id == id);

        return employee is null ? NotFound() : Ok(ToEmployeeDto(employee));
    }

    [HttpGet("department/{departmentId:int}")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetByDepartment(int departmentId)
    {
        var departmentExists = await _context.Departments.AnyAsync(department => department.Id == departmentId);
        if (!departmentExists)
        {
            return NotFound();
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Where(employee => employee.DepartmentId == departmentId)
            .OrderBy(employee => employee.FullName)
            .Select(employee => ToEmployeeDto(employee))
            .ToListAsync();

        return Ok(employees);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeDto request)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(existingEmployee => existingEmployee.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        var departmentExists = await _context.Departments.AnyAsync(department => department.Id == request.DepartmentId);
        if (!departmentExists)
        {
            return BadRequest(new { message = "Department does not exist." });
        }

        var employeeNumberExists = await _context.Employees.AnyAsync(existingEmployee =>
            existingEmployee.Id != id && existingEmployee.EmployeeNumber == request.EmployeeNumber);
        if (employeeNumberExists)
        {
            return Conflict(new { message = "Employee number already exists." });
        }

        employee.EmployeeNumber = request.EmployeeNumber;
        employee.FullName = request.FullName;
        employee.Email = request.Email;
        employee.PhoneNumber = request.PhoneNumber;
        employee.Position = request.Position;
        employee.DepartmentId = request.DepartmentId;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Update, nameof(Employee), employee.Id, null, "Employee updated.");

        var updated = await GetEmployeeEntity(employee.Id);
        return Ok(ToEmployeeDto(updated!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(existingEmployee => existingEmployee.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        var hasVisits = await _context.Visits.AnyAsync(visit => visit.HostEmployeeId == id);
        if (hasVisits)
        {
            return Conflict(new { message = "Cannot delete employee with existing visits." });
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(AuditAction.Delete, nameof(Employee), id, null, "Employee deleted.");

        return NoContent();
    }

    private Task<Employee?> GetEmployeeEntity(int id)
    {
        return _context.Employees
            .Include(employee => employee.Department)
            .FirstOrDefaultAsync(employee => employee.Id == id);
    }

    private static EmployeeDto ToEmployeeDto(Employee employee) => new()
    {
        Id = employee.Id,
        EmployeeNumber = employee.EmployeeNumber,
        FullName = employee.FullName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        Position = employee.Position,
        DepartmentId = employee.DepartmentId,
        DepartmentName = employee.Department?.Name ?? string.Empty,
        IsActive = employee.IsActive,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt
    };
}
