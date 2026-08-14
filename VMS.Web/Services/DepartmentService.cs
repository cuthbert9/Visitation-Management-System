using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class DepartmentService(HttpClient httpClient) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<DepartmentDto>>("api/departments") ?? [];
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<DepartmentDto>($"api/departments/{id}");
    }
}
