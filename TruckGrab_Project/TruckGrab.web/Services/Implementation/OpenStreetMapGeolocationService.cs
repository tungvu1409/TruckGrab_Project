using System.Text.Json;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Implementation;

public class OpenStreetMapGeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenStreetMapGeolocationService> _logger;

    public OpenStreetMapGeolocationService(HttpClient httpClient, ILogger<OpenStreetMapGeolocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeocodingResult> GeocodeAddressAsync(string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new GeocodingResult { Success = false, ErrorMessage = "Address cannot be empty" };
            }

            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "TruckGrabApp/1.0");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OSM geocoding request failed with status {StatusCode}", response.StatusCode);
                return new GeocodingResult { Success = false, ErrorMessage = "Failed to call OpenStreetMap geocoding" };
            }

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<JsonElement>>(content);

            if (results == null || results.Count == 0)
            {
                return new GeocodingResult { Success = false, ErrorMessage = "No results found for the address" };
            }

            var first = results[0];
            if (!first.TryGetProperty("lat", out var latElement) || !first.TryGetProperty("lon", out var lonElement))
            {
                return new GeocodingResult { Success = false, ErrorMessage = "Invalid response from geocoding service" };
            }

            var lat = latElement.GetString();
            var lon = lonElement.GetString();
            var displayName = first.GetProperty("display_name").GetString() ?? address;

            return new GeocodingResult
            {
                Success = true,
                Coordinates = new GeoPoint { Latitude = decimal.Parse(lat!), Longitude = decimal.Parse(lon!) },
                Address = address,
                FormattedAddress = displayName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return new GeocodingResult { Success = false, ErrorMessage = "An error occurred while geocoding" };
        }
    }

    public async Task<GeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "TruckGrabApp/1.0");
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OSM reverse geocoding request failed with status {StatusCode}", response.StatusCode);
                return new GeocodingResult { Success = false, ErrorMessage = "Failed to call OpenStreetMap reverse geocoding" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (!root.TryGetProperty("display_name", out var displayNameElement))
            {
                return new GeocodingResult { Success = false, ErrorMessage = "No address found for coordinates" };
            }

            return new GeocodingResult
            {
                Success = true,
                Coordinates = new GeoPoint { Latitude = latitude, Longitude = longitude },
                Address = displayNameElement.GetString() ?? string.Empty,
                FormattedAddress = displayNameElement.GetString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Latitude},{Longitude}", latitude, longitude);
            return new GeocodingResult { Success = false, ErrorMessage = "An error occurred while reverse geocoding" };
        }
    }

    public async Task<DistanceMatrixResult> GetDistanceAsync(GeoPoint origin, GeoPoint destination)
    {
        var distanceMeters = (int)(CalculateHaversineDistance(origin, destination) * 1000);
        var durationSeconds = (int)(distanceMeters / (14.0m) );

        return new DistanceMatrixResult
        {
            Success = true,
            DistanceMeters = distanceMeters,
            DurationSeconds = durationSeconds,
            DistanceText = $"{distanceMeters / 1000.0m:F1} km",
            DurationText = $"{TimeSpan.FromSeconds(durationSeconds):hh\\:mm\\:ss}"
        };
    }

    public async Task<DistanceMatrixResult> GetDistanceByAddressAsync(string originAddress, string destinationAddress)
    {
        var originResult = await GeocodeAddressAsync(originAddress);
        var destinationResult = await GeocodeAddressAsync(destinationAddress);

        if (!originResult.Success || originResult.Coordinates == null || !destinationResult.Success || destinationResult.Coordinates == null)
        {
            return new DistanceMatrixResult { Success = false, ErrorMessage = "Unable to geocode one or both addresses" };
        }

        return await GetDistanceAsync(originResult.Coordinates, destinationResult.Coordinates);
    }

    public async Task<RouteInfo> GetRouteAsync(GeoPoint origin, GeoPoint destination)
    {
        var distanceMeters = (int)(CalculateHaversineDistance(origin, destination) * 1000);
        var durationSeconds = (int)(distanceMeters / 14.0m);

        return new RouteInfo
        {
            Success = true,
            DistanceMeters = distanceMeters,
            DurationSeconds = durationSeconds,
            Steps = new List<GeoPoint> { origin, destination },
            PolylinePoints = null
        };
    }

    public decimal CalculateHaversineDistance(GeoPoint point1, GeoPoint point2)
    {
        const decimal EarthRadiusKm = 6371m;
        var lat1 = DegreesToRadians(point1.Latitude);
        var lon1 = DegreesToRadians(point1.Longitude);
        var lat2 = DegreesToRadians(point2.Latitude);
        var lon2 = DegreesToRadians(point2.Longitude);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Pow(Math.Sin((double)(dLat / 2)), 2) +
                Math.Cos((double)lat1) * Math.Cos((double)lat2) *
                Math.Pow(Math.Sin((double)(dLon / 2)), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * (decimal)c;
    }

    private static decimal DegreesToRadians(decimal degrees) => degrees * (decimal)Math.PI / 180m;
}
