using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Models;
using VisitorManagementSystem.Domain.Entities;
using VisitorManagementSystem.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentDto request)
    {
        var officeExists = await _context.Offices.AnyAsync(office => office.Id == request.OfficeId);
        if (!officeExists)
        {
            return BadRequest(new { message = "Office does not exist." });
        }

        var now = DateTime.UtcNow;
        var department = new Department
        {
            Name = request.Name,
            Description = request.Description,
            OfficeId = request.OfficeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

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

    [HttpGet("office/{officeId:int}")]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetByOffice(int officeId)
    {
        var officeExists = await _context.Offices.AnyAsync(office => office.Id == officeId);
        if (!officeExists)
        {
            return NotFound();
        }

        var departments = await _context.Departments
            .AsNoTracking()
            .Where(department => department.OfficeId == officeId)
            .OrderBy(department => department.Name)
            .Select(department => ToDepartmentDto(department))
            .ToListAsync();

        return Ok(departments);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] UpdateDepartmentDto request)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(existingDepartment => existingDepartment.Id == id);
        if (department is null)
        {
            return NotFound();
        }

        var officeExists = await _context.Offices.AnyAsync(office => office.Id == request.OfficeId);
        if (!officeExists)
        {
            return BadRequest(new { message = "Office does not exist." });
        }

        department.Name = request.Name;
        department.Description = request.Description;
        department.OfficeId = request.OfficeId;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
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

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static DepartmentDto ToDepartmentDto(Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        OfficeId = department.OfficeId,
        CreatedAt = department.CreatedAt,
        UpdatedAt = department.UpdatedAt
    };
}
