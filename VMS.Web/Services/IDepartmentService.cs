using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id);
}
