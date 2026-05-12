using System.Collections.Concurrent;

namespace TruckGrab.web.Services;

/// <summary>
/// In-memory store for driver GPS locations.
/// Data lives only for the lifetime of the server process.
/// Key = DriverId
/// </summary>
public class DriverLocationStore
{
    private readonly ConcurrentDictionary<int, DriverLocation> _locations = new();

    public void UpdateLocation(int driverId, string driverName, string status, double lat, double lng)
    {
        _locations[driverId] = new DriverLocation
        {
            DriverId  = driverId,
            Name      = driverName,
            Status    = status,
            Lat       = lat,
            Lng       = lng,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void RemoveLocation(int driverId)
    {
        _locations.TryRemove(driverId, out _);
    }

    public DriverLocation? GetLocation(int driverId)
        => _locations.TryGetValue(driverId, out var loc) ? loc : null;

    /// <summary>Returns drivers that have updated their location within the last <paramref name="maxAgeMinutes"/> minutes.</summary>
    public IEnumerable<DriverLocation> GetOnlineDrivers(int maxAgeMinutes = 5)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-maxAgeMinutes);
        return _locations.Values.Where(l => l.UpdatedAt >= cutoff);
    }
}

public class DriverLocation
{
    public int    DriverId  { get; set; }
    public string Name      { get; set; } = string.Empty;
    public string Status    { get; set; } = string.Empty;
    public double Lat       { get; set; }
    public double Lng       { get; set; }
    public DateTime UpdatedAt { get; set; }
}
