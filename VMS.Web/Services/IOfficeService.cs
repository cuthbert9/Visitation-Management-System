using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public interface IOfficeService
{
    Task<IReadOnlyList<OfficeDto>> GetOfficesAsync();
}
