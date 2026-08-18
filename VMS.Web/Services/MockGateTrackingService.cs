using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

/// <summary>
/// Mock only: no backend concept of device/PC tracking exists yet. Holds gate-only
/// data in memory, keyed by the real Visit's Id, for the lifetime of the app session.
/// </summary>
public class MockGateTrackingService : IGateTrackingService
{
    private readonly Dictionary<int, GateTrackingInfo> _info = [];

    public Task<GateTrackingInfo?> GetInfoAsync(int visitId)
    {
        _info.TryGetValue(visitId, out var info);
        return Task.FromResult(info);
    }

    public Task SaveInfoAsync(int visitId, bool hasLaptop, string? deviceType, string? deviceBrandOrAssetTag)
    {
        _info[visitId] = new GateTrackingInfo
        {
            HasLaptop = hasLaptop,
            DeviceType = hasLaptop ? deviceType : null,
            DeviceBrandOrAssetTag = hasLaptop ? deviceBrandOrAssetTag : null,
            PcConfirmedReturned = false
        };

        return Task.CompletedTask;
    }

    public bool HasPcMismatch(int visitId, VisitStatus status)
    {
        if (!_info.TryGetValue(visitId, out var info) || !info.HasLaptop)
        {
            return false;
        }

        var isFinished = status is VisitStatus.Completed or VisitStatus.Closed;
        return isFinished && !info.PcConfirmedReturned;
    }
}
