using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync();
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId);
    Task<EmployeeDto?> CreateEmployeeAsync(CreateEmployeeDto request);
    Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto request);
    Task DeleteEmployeeAsync(int id);
}
