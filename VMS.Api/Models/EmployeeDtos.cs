using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Models;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmployeeDto
{
    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }

    [Required]
    public int DepartmentId { get; set; }
}

public class UpdateEmployeeDto
{
    [Required]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }

    [Required]
    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}
