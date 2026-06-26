using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IOfficeService
{
    Task<IReadOnlyList<OfficeDto>> GetOfficesAsync();
    Task<OfficeDto?> GetOfficeByIdAsync(int id);
    Task<OfficeDto?> CreateOfficeAsync(CreateOfficeDto request);
    Task<OfficeDto?> UpdateOfficeAsync(int id, UpdateOfficeDto request);
    Task DeleteOfficeAsync(int id);
    Task<IReadOnlyList<VisitDto>> GetOfficeVisitsAsync(int id);
}
