using System.Net;
using System.Net.Http.Json;
using VisitorManagementSystem.Shared.Models;

namespace VMS.Web.Services;

public class GateTrackingService(HttpClient httpClient) : IGateTrackingService
{
    private readonly Dictionary<int, GateTrackingInfo?> _cache = [];

    public async Task<GateTrackingInfo?> GetInfoAsync(int visitId)
    {
        var response = await httpClient.GetAsync($"api/visitequipment/visit/{visitId}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _cache[visitId] = null;
            return null;
        }

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<VisitEquipmentDto>();
        var info = dto is null ? null : new GateTrackingInfo
        {
            HasLaptop = dto.HasLaptop,
            DeviceType = dto.DeviceType,
            DeviceBrand = dto.DeviceBrand,
            AssetTag = dto.AssetTag,
            PcConfirmedReturned = dto.PcConfirmedReturned
        };

        _cache[visitId] = info;
        return info;
    }

    public async Task SaveInfoAsync(int visitId, bool hasLaptop, string? deviceType, string? deviceBrand, string? assetTag)
    {
        var response = await httpClient.PostAsJsonAsync("api/visitequipment", new CreateVisitEquipmentDto
        {
            VisitId = visitId,
            HasLaptop = hasLaptop,
            DeviceType = deviceType,
            DeviceBrand = deviceBrand,
            AssetTag = assetTag
        });

        response.EnsureSuccessStatusCode();

        _cache[visitId] = new GateTrackingInfo
        {
            HasLaptop = hasLaptop,
            DeviceType = hasLaptop ? deviceType : null,
            DeviceBrand = hasLaptop ? deviceBrand : null,
            AssetTag = hasLaptop ? assetTag : null,
            PcConfirmedReturned = false
        };
    }

    public bool HasPcMismatch(int visitId, VisitStatus status)
    {
        if (!_cache.TryGetValue(visitId, out var info) || info is null || !info.HasLaptop)
        {
            return false;
        }

        var isFinished = status is VisitStatus.Completed or VisitStatus.Closed;
        return isFinished && !info.PcConfirmedReturned;
    }
}
