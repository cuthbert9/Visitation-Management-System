using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class GateTrackingInfo
{
    public bool HasLaptop { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceBrandOrAssetTag { get; set; }
    public bool PcConfirmedReturned { get; set; }
}

public interface IGateTrackingService
{
    Task<GateTrackingInfo?> GetInfoAsync(int visitId);
    Task SaveInfoAsync(int visitId, bool hasLaptop, string? deviceType, string? deviceBrandOrAssetTag);
    bool HasPcMismatch(int visitId, VisitStatus status);
}
