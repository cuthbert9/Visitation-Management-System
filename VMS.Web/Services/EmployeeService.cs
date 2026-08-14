using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class EmployeeService(HttpClient httpClient) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<EmployeeDto>>("api/employees") ?? [];
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<EmployeeDto>($"api/employees/{id}");
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesByDepartmentAsync(int departmentId)
    {
        return await httpClient.GetFromJsonAsync<List<EmployeeDto>>($"api/employees/department/{departmentId}") ?? [];
    }

    public async Task<EmployeeDto?> CreateEmployeeAsync(CreateEmployeeDto request)
    {
        var response = await httpClient.PostAsJsonAsync("api/employees", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeDto>();
    }

    public async Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto request)
    {
        var response = await httpClient.PutAsJsonAsync($"api/employees/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmployeeDto>();
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"api/employees/{id}");
        response.EnsureSuccessStatusCode();
    }
}
