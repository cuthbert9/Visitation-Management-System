using Microsoft.AspNetCore.Components.Forms;

namespace VMS.Web.Services;

public interface IStorageService
{
    Task<string> UploadAsync(IBrowserFile file);
}
