using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Helpers;

public class GeolocationHelper
{
    private readonly IGeolocationService _geolocationService;
    private readonly ILogger<GeolocationHelper> _logger;

    public GeolocationHelper(IGeolocationService geolocationService, ILogger<GeolocationHelper> logger)
    {
        _geolocationService = geolocationService;
        _logger = logger;
    }

    public async Task<OrderDeliveryEstimate> EstimateDeliveryAsync(Location pickupLocation, Location deliveryLocation, decimal cargoWeight)
    {
        try
        {
            var origin = new GeoPoint 
            { 
                Latitude = pickupLocation.Lat, 
                Longitude = pickupLocation.Lng 
            };
            var destination = new GeoPoint 
            { 
                Latitude = deliveryLocation.Lat, 
                Longitude = deliveryLocation.Lng 
            };

            var distanceResult = await _geolocationService.GetDistanceAsync(origin, destination);

            if (!distanceResult.Success)
            {
                _logger.LogError("Failed to calculate distance for order estimation");
                return new OrderDeliveryEstimate { Success = false };
            }

            var distanceKm = distanceResult.DistanceMeters / 1000m;
            var baseRate = 50m;
            var ratePerKm = 5m;
            var ratePerKg = 0.5m;

            var cost = baseRate + (distanceKm * ratePerKm) + (cargoWeight * ratePerKg);

            return new OrderDeliveryEstimate
            {
                Success = true,
                DistanceKm = distanceKm,
                DistanceMeters = distanceResult.DistanceMeters,
                DurationSeconds = distanceResult.DurationSeconds,
                DurationMinutes = distanceResult.DurationSeconds / 60,
                EstimatedCost = Math.Round(cost, 2),
                DistanceText = distanceResult.DistanceText,
                DurationText = distanceResult.DurationText
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating delivery for order");
            return new OrderDeliveryEstimate { Success = false };
        }
    }

    public async Task<bool> IsDriverNearbyAsync(Location driverLocation, Location pickupLocation, decimal maxDistanceKm = 50)
    {
        try
        {
            var origin = new GeoPoint 
            { 
                Latitude = driverLocation.Lat, 
                Longitude = driverLocation.Lng 
            };
            var destination = new GeoPoint 
            { 
                Latitude = pickupLocation.Lat, 
                Longitude = pickupLocation.Lng 
            };

            var distanceResult = await _geolocationService.GetDistanceAsync(origin, destination);

            if (!distanceResult.Success)
            {
                return false;
            }

            var distanceKm = distanceResult.DistanceMeters / 1000m;
            return distanceKm <= maxDistanceKm;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if driver is nearby");
            return false;
        }
    }

    public async Task<Driver?> FindNearestDriverAsync(Location pickupLocation, List<Driver> availableDrivers, Location? driverLocation = null)
    {
        try
        {
            if (availableDrivers.Count == 0)
            {
                return null;
            }

            var pickupPoint = new GeoPoint 
            { 
                Latitude = pickupLocation.Lat, 
                Longitude = pickupLocation.Lng 
            };

            var driverDistances = new Dictionary<Driver, decimal>();

            foreach (var driver in availableDrivers)
            {
                var driverPoint = driverLocation != null 
                    ? new GeoPoint { Latitude = driverLocation.Lat, Longitude = driverLocation.Lng }
                    : new GeoPoint { Latitude = 0, Longitude = 0 }; // Default

                if (driverPoint.Latitude != 0 || driverPoint.Longitude != 0)
                {
                    var distance = _geolocationService.CalculateHaversineDistance(driverPoint, pickupPoint);
                    driverDistances[driver] = distance;
                }
            }

            if (driverDistances.Count == 0)
            {
                return availableDrivers.FirstOrDefault();
            }

            return driverDistances.OrderBy(x => x.Value).First().Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearest driver");
            return availableDrivers.FirstOrDefault();
        }
    }

    public async Task<GeoPoint?> GeocodeAddressAsync(string address)
    {
        try
        {
            var result = await _geolocationService.GeocodeAddressAsync(address);
            return result.Success ? result.Coordinates : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return null;
        }
    }

    public async Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        try
        {
            var result = await _geolocationService.ReverseGeocodeAsync(latitude, longitude);
            return result.Success ? result.FormattedAddress : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding coordinates");
            return null;
        }
    }
}

public class OrderDeliveryEstimate
{
    public bool Success { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    public int DurationMinutes { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? DistanceText { get; set; }
    public string? DurationText { get; set; }
}
