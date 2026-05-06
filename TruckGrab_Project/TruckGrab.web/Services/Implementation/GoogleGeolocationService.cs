using System.Text.Json;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Implementation;

public class GoogleGeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleGeolocationService> _logger;
    private readonly string _apiKey;

    private const string GeocodingUrl = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string DistanceMatrixUrl = "https://maps.googleapis.com/maps/api/distancematrix/json";
    private const string DirectionsUrl = "https://maps.googleapis.com/maps/api/directions/json";

    public GoogleGeolocationService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleGeolocationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["GoogleMaps:ApiKey"] ?? string.Empty;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Google Maps API Key is not configured");
        }
    }

    public async Task<GeocodingResult> GeocodeAddressAsync(string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new GeocodingResult { Success = false, ErrorMessage = "Address cannot be empty" };
            }

            var url = $"{GeocodingUrl}?address={Uri.EscapeDataString(address)}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Geocoding API returned status code {StatusCode}", response.StatusCode);
                return new GeocodingResult { Success = false, ErrorMessage = "Failed to call Google Maps API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    _logger.LogWarning("Google Geocoding API returned status: {Status}", status);
                    return new GeocodingResult { Success = false, ErrorMessage = $"Geocoding failed: {status}" };
                }

                var results = root.GetProperty("results");
                if (results.GetArrayLength() == 0)
                {
                    return new GeocodingResult { Success = false, ErrorMessage = "No results found for the address" };
                }

                var firstResult = results[0];
                var location = firstResult.GetProperty("geometry").GetProperty("location");
                var formattedAddress = firstResult.GetProperty("formatted_address").GetString();

                var lat = location.GetProperty("lat").GetDecimal();
                var lng = location.GetProperty("lng").GetDecimal();

                return new GeocodingResult
                {
                    Success = true,
                    Coordinates = new GeoPoint { Latitude = lat, Longitude = lng },
                    Address = address,
                    FormattedAddress = formattedAddress
                };
            }
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
            var url = $"{GeocodingUrl}?latlng={latitude},{longitude}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Reverse Geocoding API returned status code {StatusCode}", response.StatusCode);
                return new GeocodingResult { Success = false, ErrorMessage = "Failed to call Google Maps API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    _logger.LogWarning("Google Reverse Geocoding API returned status: {Status}", status);
                    return new GeocodingResult { Success = false, ErrorMessage = $"Reverse geocoding failed: {status}" };
                }

                var results = root.GetProperty("results");
                if (results.GetArrayLength() == 0)
                {
                    return new GeocodingResult { Success = false, ErrorMessage = "No results found for the coordinates" };
                }

                var firstResult = results[0];
                var formattedAddress = firstResult.GetProperty("formatted_address").GetString();

                return new GeocodingResult
                {
                    Success = true,
                    Coordinates = new GeoPoint { Latitude = latitude, Longitude = longitude },
                    Address = formattedAddress ?? string.Empty,
                    FormattedAddress = formattedAddress
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates: {Latitude},{Longitude}", latitude, longitude);
            return new GeocodingResult { Success = false, ErrorMessage = "An error occurred while reverse geocoding" };
        }
    }

    public async Task<DistanceMatrixResult> GetDistanceAsync(GeoPoint origin, GeoPoint destination)
    {
        try
        {
            var originStr = $"{origin.Latitude},{origin.Longitude}";
            var destinationStr = $"{destination.Latitude},{destination.Longitude}";

            var url = $"{DistanceMatrixUrl}?origins={originStr}&destinations={destinationStr}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Distance Matrix API returned status code {StatusCode}", response.StatusCode);
                return new DistanceMatrixResult { Success = false, ErrorMessage = "Failed to call Google Maps API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    _logger.LogWarning("Google Distance Matrix API returned status: {Status}", status);
                    return new DistanceMatrixResult { Success = false, ErrorMessage = $"Distance calculation failed: {status}" };
                }

                var rows = root.GetProperty("rows");
                var elements = rows[0].GetProperty("elements");
                var element = elements[0];

                var elementStatus = element.GetProperty("status").GetString();
                if (elementStatus != "OK")
                {
                    return new DistanceMatrixResult { Success = false, ErrorMessage = "Unable to calculate distance" };
                }

                var distance = element.GetProperty("distance").GetProperty("value").GetInt32();
                var duration = element.GetProperty("duration").GetProperty("value").GetInt32();
                var distanceText = element.GetProperty("distance").GetProperty("text").GetString();
                var durationText = element.GetProperty("duration").GetProperty("text").GetString();

                return new DistanceMatrixResult
                {
                    Success = true,
                    DistanceMeters = distance,
                    DurationSeconds = duration,
                    DistanceText = distanceText,
                    DurationText = durationText
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance between points");
            return new DistanceMatrixResult { Success = false, ErrorMessage = "An error occurred while calculating distance" };
        }
    }

    public async Task<DistanceMatrixResult> GetDistanceByAddressAsync(string originAddress, string destinationAddress)
    {
        try
        {
            var url = $"{DistanceMatrixUrl}?origins={Uri.EscapeDataString(originAddress)}&destinations={Uri.EscapeDataString(destinationAddress)}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Distance Matrix API returned status code {StatusCode}", response.StatusCode);
                return new DistanceMatrixResult { Success = false, ErrorMessage = "Failed to call Google Maps API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    _logger.LogWarning("Google Distance Matrix API returned status: {Status}", status);
                    return new DistanceMatrixResult { Success = false, ErrorMessage = $"Distance calculation failed: {status}" };
                }

                var rows = root.GetProperty("rows");
                var elements = rows[0].GetProperty("elements");
                var element = elements[0];

                var elementStatus = element.GetProperty("status").GetString();
                if (elementStatus != "OK")
                {
                    return new DistanceMatrixResult { Success = false, ErrorMessage = "Unable to calculate distance" };
                }

                var distance = element.GetProperty("distance").GetProperty("value").GetInt32();
                var duration = element.GetProperty("duration").GetProperty("value").GetInt32();
                var distanceText = element.GetProperty("distance").GetProperty("text").GetString();
                var durationText = element.GetProperty("duration").GetProperty("text").GetString();

                return new DistanceMatrixResult
                {
                    Success = true,
                    DistanceMeters = distance,
                    DurationSeconds = duration,
                    DistanceText = distanceText,
                    DurationText = durationText
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance by address");
            return new DistanceMatrixResult { Success = false, ErrorMessage = "An error occurred while calculating distance" };
        }
    }

    public async Task<RouteInfo> GetRouteAsync(GeoPoint origin, GeoPoint destination)
    {
        try
        {
            var originStr = $"{origin.Latitude},{origin.Longitude}";
            var destinationStr = $"{destination.Latitude},{destination.Longitude}";

            var url = $"{DirectionsUrl}?origin={originStr}&destination={destinationStr}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Directions API returned status code {StatusCode}", response.StatusCode);
                return new RouteInfo { Success = false, ErrorMessage = "Failed to call Google Maps API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status != "OK")
                {
                    _logger.LogWarning("Google Directions API returned status: {Status}", status);
                    return new RouteInfo { Success = false, ErrorMessage = $"Route calculation failed: {status}" };
                }

                var routes = root.GetProperty("routes");
                if (routes.GetArrayLength() == 0)
                {
                    return new RouteInfo { Success = false, ErrorMessage = "No routes found" };
                }

                var route = routes[0];
                var legs = route.GetProperty("legs");
                var leg = legs[0];

                var distance = leg.GetProperty("distance").GetProperty("value").GetInt32();
                var duration = leg.GetProperty("duration").GetProperty("value").GetInt32();
                var polyline = route.GetProperty("overview_polyline").GetProperty("points").GetString();

                var steps = new List<GeoPoint>();
                var stepsArray = leg.GetProperty("steps");
                foreach (var step in stepsArray.EnumerateArray())
                {
                    var startLocation = step.GetProperty("start_location");
                    var lat = startLocation.GetProperty("lat").GetDecimal();
                    var lng = startLocation.GetProperty("lng").GetDecimal();
                    steps.Add(new GeoPoint { Latitude = lat, Longitude = lng });
                }

                return new RouteInfo
                {
                    Success = true,
                    DistanceMeters = distance,
                    DurationSeconds = duration,
                    Steps = steps,
                    PolylinePoints = polyline
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route");
            return new RouteInfo { Success = false, ErrorMessage = "An error occurred while getting route" };
        }
    }

    public decimal CalculateHaversineDistance(GeoPoint point1, GeoPoint point2)
    {
        const decimal R = 6371m; // Earth's radius in kilometers

        var lat1Rad = ConvertToRadians(point1.Latitude);
        var lat2Rad = ConvertToRadians(point2.Latitude);
        var deltaLat = ConvertToRadians(point2.Latitude - point1.Latitude);
        var deltaLng = ConvertToRadians(point2.Longitude - point1.Longitude);

        var a = decimal.Parse(Math.Sin((double)deltaLat / 2).ToString()) * decimal.Parse(Math.Sin((double)deltaLat / 2).ToString()) +
                decimal.Parse(Math.Cos((double)lat1Rad).ToString()) * decimal.Parse(Math.Cos((double)lat2Rad).ToString()) *
                decimal.Parse(Math.Sin((double)deltaLng / 2).ToString()) * decimal.Parse(Math.Sin((double)deltaLng / 2).ToString());

        var c = 2 * decimal.Parse(Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a))).ToString());
        var distance = R * c;

        return distance;
    }

    private decimal ConvertToRadians(decimal degrees)
    {
        return degrees * (decimal.Parse(Math.PI.ToString()) / 180);
    }
}
