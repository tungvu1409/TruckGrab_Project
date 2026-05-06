namespace TruckGrab.web.Services.Interface;

public class GeoPoint
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public class GeocodingResult
{
    public bool Success { get; set; }
    public GeoPoint? Coordinates { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? FormattedAddress { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DistanceMatrixResult
{
    public bool Success { get; set; }
    public decimal DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    public string? DistanceText { get; set; }
    public string? DurationText { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RouteInfo
{
    public bool Success { get; set; }
    public decimal DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    public List<GeoPoint> Steps { get; set; } = new();
    public string? PolylinePoints { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IGeolocationService
{
    /// <summary>
    /// Geocodes an address to coordinates
    /// </summary>
    Task<GeocodingResult> GeocodeAddressAsync(string address);

    /// <summary>
    /// Reverse geocodes coordinates to an address
    /// </summary>
    Task<GeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude);

    /// <summary>
    /// Calculates distance and duration between two points
    /// </summary>
    Task<DistanceMatrixResult> GetDistanceAsync(GeoPoint origin, GeoPoint destination);

    /// <summary>
    /// Calculates distance and duration between address strings
    /// </summary>
    Task<DistanceMatrixResult> GetDistanceByAddressAsync(string originAddress, string destinationAddress);

    /// <summary>
    /// Gets route information between two points
    /// </summary>
    Task<RouteInfo> GetRouteAsync(GeoPoint origin, GeoPoint destination);

    /// <summary>
    /// Calculates distance between two coordinates in kilometers
    /// </summary>
    decimal CalculateHaversineDistance(GeoPoint point1, GeoPoint point2);
}
