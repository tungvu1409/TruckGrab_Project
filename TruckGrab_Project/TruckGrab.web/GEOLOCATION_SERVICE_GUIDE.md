# Google Geolocation Service Implementation

## Overview
This documentation explains how to use the Google Geolocation Service integrated into the TruckGrab application.

## Setup

### 1. Get Google Maps API Key
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project
3. Enable the following APIs:
   - Maps JavaScript API
   - Geocoding API
   - Distance Matrix API
   - Directions API
4. Create an API key (Credentials → Create Credentials → API Key)
5. Restrict the key to your application (Optional but recommended)

### 2. Configure API Key
Update `appsettings.json`:
```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_ACTUAL_GOOGLE_MAPS_API_KEY_HERE"
  }
}
```

Also update `appsettings.Development.json` for development environment.

## Features

### 1. Geocoding (Address to Coordinates)
Convert a street address to latitude and longitude.

```csharp
public class OrderController : Controller
{
    private readonly IGeolocationService _geolocationService;

    public OrderController(IGeolocationService geolocationService)
    {
        _geolocationService = geolocationService;
    }

    public async Task<IActionResult> CreateOrder(string pickupAddress)
    {
        var result = await _geolocationService.GeocodeAddressAsync(pickupAddress);
        
        if (result.Success)
        {
            var latitude = result.Coordinates.Latitude;
            var longitude = result.Coordinates.Longitude;
            var formattedAddress = result.FormattedAddress;
            // Use coordinates for your order
        }
        else
        {
            // Handle error: result.ErrorMessage
        }
    }
}
```

### 2. Reverse Geocoding (Coordinates to Address)
Convert latitude and longitude to a readable address.

```csharp
var result = await _geolocationService.ReverseGeocodeAsync(40.7128m, -74.0060m);

if (result.Success)
{
    var address = result.FormattedAddress; // "New York, NY, USA"
}
```

### 3. Distance Calculation by Coordinates
Calculate distance and duration between two points.

```csharp
var origin = new GeoPoint 
{ 
    Latitude = 40.7128m, 
    Longitude = -74.0060m 
};
var destination = new GeoPoint 
{ 
    Latitude = 34.0522m, 
    Longitude = -118.2437m 
};

var result = await _geolocationService.GetDistanceAsync(origin, destination);

if (result.Success)
{
    var distanceKm = result.DistanceMeters / 1000;
    var durationMinutes = result.DurationSeconds / 60;
    Console.WriteLine($"Distance: {result.DistanceText}");
    Console.WriteLine($"Duration: {result.DurationText}");
}
```

### 4. Distance Calculation by Address
Calculate distance between address strings.

```csharp
var result = await _geolocationService.GetDistanceByAddressAsync(
    "123 Main St, New York, NY",
    "456 Oak Ave, Los Angeles, CA"
);

if (result.Success)
{
    var distance = result.DistanceText;      // "2,792 km"
    var duration = result.DurationText;      // "1 day 2 hours"
}
```

### 5. Route Information
Get detailed route information including waypoints and polyline.

```csharp
var result = await _geolocationService.GetRouteAsync(origin, destination);

if (result.Success)
{
    var distance = result.DistanceMeters;
    var steps = result.Steps;                // List of waypoints
    var polyline = result.PolylinePoints;    // For map visualization
}
```

### 6. Haversine Distance (Offline Calculation)
Calculate distance between two points without API calls (useful for client-side calculations).

```csharp
var point1 = new GeoPoint { Latitude = 40.7128m, Longitude = -74.0060m };
var point2 = new GeoPoint { Latitude = 34.0522m, Longitude = -118.2437m };

var distanceKm = _geolocationService.CalculateHaversineDistance(point1, point2);
// Returns approximate distance in kilometers
```

## Using GeolocationHelper

The `GeolocationHelper` class provides high-level business logic for common TruckGrab operations.

### 1. Estimate Delivery Cost and Time
```csharp
public class OrderService
{
    private readonly GeolocationHelper _geolocationHelper;

    public async Task<OrderDeliveryEstimate> EstimateOrderAsync(Location pickupLocation, Location deliveryLocation, decimal weight)
    {
        var estimate = await _geolocationHelper.EstimateDeliveryAsync(
            pickupLocation, 
            deliveryLocation, 
            weight
        );

        if (estimate.Success)
        {
            Console.WriteLine($"Distance: {estimate.DistanceKm} km");
            Console.WriteLine($"Duration: {estimate.DurationMinutes} minutes");
            Console.WriteLine($"Estimated Cost: ${estimate.EstimatedCost}");
        }

        return estimate;
    }
}
```

### 2. Check if Driver is Nearby
```csharp
public async Task<bool> IsDriverAvailableForOrderAsync(Driver driver, Location pickupLocation)
{
    var driverLocation = new Location 
    { 
        Lat = 40.7128m, 
        Lng = -74.0060m 
    };

    return await _geolocationHelper.IsDriverNearbyAsync(
        driverLocation, 
        pickupLocation, 
        maxDistanceKm: 50
    );
}
```

### 3. Find Nearest Available Driver
```csharp
public async Task<Driver?> AssignNearestDriverAsync(Order order, List<Driver> availableDrivers)
{
    var pickupLocation = // Get from order
    var driverLocation = // Get driver's current location

    var nearestDriver = await _geolocationHelper.FindNearestDriverAsync(
        pickupLocation,
        availableDrivers,
        driverLocation
    );

    return nearestDriver;
}
```

## Integration Examples

### Example 1: Create Order with Location Geocoding
```csharp
[HttpPost]
public async Task<IActionResult> CreateOrder(
    string pickupAddress,
    string deliveryAddress,
    decimal weight)
{
    // Geocode addresses
    var pickupResult = await _geolocationService.GeocodeAddressAsync(pickupAddress);
    var deliveryResult = await _geolocationService.GeocodeAddressAsync(deliveryAddress);

    if (!pickupResult.Success || !deliveryResult.Success)
    {
        return BadRequest("Invalid addresses");
    }

    // Create location records
    var pickupLocation = new Location
    {
        Name = "Pickup Point",
        Address = pickupResult.FormattedAddress,
        Lat = pickupResult.Coordinates.Latitude,
        Lng = pickupResult.Coordinates.Longitude,
        Type = LocationType.CustomerPoint
    };

    var deliveryLocation = new Location
    {
        Name = "Delivery Point",
        Address = deliveryResult.FormattedAddress,
        Lat = deliveryResult.Coordinates.Latitude,
        Lng = deliveryResult.Coordinates.Longitude,
        Type = LocationType.CustomerPoint
    };

    // Estimate delivery
    var estimate = await _geolocationHelper.EstimateDeliveryAsync(
        pickupLocation,
        deliveryLocation,
        weight
    );

    // Create order
    var order = new Order
    {
        PickupLocId = pickupLocation.Id,
        DeliveryLocId = deliveryLocation.Id,
        Weight = weight,
        EstimatedCost = estimate.EstimatedCost
    };

    // Save and return
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

### Example 2: Assign Driver Based on Location
```csharp
[HttpPost("assign-driver/{orderId}")]
public async Task<IActionResult> AssignDriver(int orderId)
{
    var order = await _context.Orders.FindAsync(orderId);
    var pickupLocation = await _context.Locations.FindAsync(order.PickupLocId);

    var availableDrivers = await _driverService.GetAvailableDriversAsync();

    var assignedDriver = await _geolocationHelper.FindNearestDriverAsync(
        pickupLocation,
        availableDrivers
    );

    if (assignedDriver == null)
    {
        return NotFound("No drivers available");
    }

    order.DriverId = assignedDriver.Id;
    await _context.SaveChangesAsync();

    return Ok(assignedDriver);
}
```

### Example 3: Real-time Distance Display
```csharp
[HttpGet("distance")]
public async Task<IActionResult> GetDistance(
    decimal originLat, 
    decimal originLng,
    decimal destLat,
    decimal destLng)
{
    var origin = new GeoPoint { Latitude = originLat, Longitude = originLng };
    var destination = new GeoPoint { Latitude = destLat, Longitude = destLng };

    var result = await _geolocationService.GetDistanceAsync(origin, destination);

    if (!result.Success)
    {
        return BadRequest(result.ErrorMessage);
    }

    return Json(new
    {
        distance = result.DistanceText,
        duration = result.DurationText,
        distanceMeters = result.DistanceMeters,
        durationSeconds = result.DurationSeconds
    });
}
```

## Database Schema Update

You may want to extend the Driver model to store current location:

```csharp
public class Driver
{
    // ... existing properties
    
    public decimal? CurrentLatitude { get; set; }
    public decimal? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
}
```

Update `ApplicationDbContext`:
```csharp
modelBuilder.Entity<Driver>(entity =>
{
    // ... existing configuration
    
    entity.Property(d => d.CurrentLatitude)
        .HasColumnName("current_latitude");
    entity.Property(d => d.CurrentLongitude)
        .HasColumnName("current_longitude");
    entity.Property(d => d.LastLocationUpdate)
        .HasColumnName("last_location_update");
});
```

## Error Handling

The service includes comprehensive error handling:

```csharp
var result = await _geolocationService.GeocodeAddressAsync(address);

if (!result.Success)
{
    // Log error
    _logger.LogError("Geocoding failed: {Error}", result.ErrorMessage);
    
    // Handle gracefully
    switch (result.ErrorMessage)
    {
        case string msg when msg.Contains("ZERO_RESULTS"):
            return BadRequest("Address not found");
        case string msg when msg.Contains("INVALID_REQUEST"):
            return BadRequest("Invalid request parameters");
        case string msg when msg.Contains("OVER_QUERY_LIMIT"):
            return StatusCode(429, "API quota exceeded");
        default:
            return StatusCode(500, "Failed to process geolocation request");
    }
}
```

## Performance Tips

1. **Cache Results**: Cache geocoding results for frequently used addresses
2. **Batch Requests**: Use Distance Matrix API for multiple distance calculations
3. **Client-side Calculation**: Use Haversine distance for approximate calculations
4. **Rate Limiting**: Implement rate limiting for API calls
5. **Async Operations**: Always use async methods to prevent blocking

## Cost Optimization

- Geocoding API: ~$5-7 per 1000 requests
- Distance Matrix API: ~$5-7 per 1000 requests
- Monitor usage in Google Cloud Console
- Set up billing alerts

## Troubleshooting

### API Key Not Working
- Verify the API key in `appsettings.json`
- Check if APIs are enabled in Google Cloud Console
- Ensure the API key has the correct restrictions

### "Zero Results" Error
- Verify the address is correctly formatted
- Try using a more complete address
- Check if the location is valid

### "Over Query Limit" Error
- Check if API quota is exceeded
- Implement caching to reduce API calls
- Consider upgrading your quota

## Testing

Example unit test:
```csharp
[Fact]
public async Task GeocodeAddress_WithValidAddress_ReturnsCoordinates()
{
    var result = await _geolocationService.GeocodeAddressAsync("1600 Amphitheatre Parkway, Mountain View, CA");
    
    Assert.True(result.Success);
    Assert.NotNull(result.Coordinates);
    Assert.True(result.Coordinates.Latitude > 0);
    Assert.True(result.Coordinates.Longitude < 0);
}
```

## Support
For issues or questions, refer to:
- [Google Maps API Documentation](https://developers.google.com/maps/documentation)
- [API Status Dashboard](https://status.cloud.google.com/)
