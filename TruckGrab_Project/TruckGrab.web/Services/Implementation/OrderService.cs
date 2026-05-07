using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Data;
using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Implementation;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeolocationService _geolocationService;

    public OrderService(ApplicationDbContext context, IGeolocationService geolocationService)
    {
        _context = context;
        _geolocationService = geolocationService;
    }

    private static class OrderStatus
    {
        public const string Pending = "pending";
        public const string Assigned = "assigned";
        public const string Picking = "picking";
        public const string InTransit = "in_transit";
        public const string Delivered = "delivered";
        public const string Cancelled = "cancelled";
    }



    // ========================
    // GET LIST
    // ========================
    public async Task<List<Order>> GetOrdersAsync(int userId)
    {
        return await _context.Orders
            .Where(o => !o.IsDeleted && o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    // ========================
    // GET DETAIL
    // ========================
    public async Task<(Order?, List<OrderStatusLog>)> GetOrderDetailAsync(int id, int userId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (order == null || order.CustomerId != userId)
            return (null, new List<OrderStatusLog>());

        var pickupLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == order.PickupLocId);

        var deliveryLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == order.DeliveryLocId);

        order.PickupLocationAddress = pickupLocation?.Address ?? string.Empty;
        order.DeliveryLocationAddress = deliveryLocation?.Address ?? string.Empty;
        order.PickupAddress = order.PickupLocationAddress;
        order.DeliveryAddress = order.DeliveryLocationAddress;

        var logs = await _context.OrderStatusLogs
            .Where(l => l.OrderId == id)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return (order, logs);
    }

    // ========================
    // GET DRIVER ORDER DETAIL
    // ========================
    public async Task<(Order?, List<OrderStatusLog>)> GetDriverOrderDetailAsync(int orderId, int driverId)
    {
        var hasTrip = await _context.Trips.AnyAsync(t => t.OrderId == orderId && t.DriverId == driverId);
        if (!hasTrip)
            return (null, new List<OrderStatusLog>());

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

        if (order == null)
            return (null, new List<OrderStatusLog>());

        var pickupLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == order.PickupLocId);

        var deliveryLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == order.DeliveryLocId);

        order.PickupLocationAddress = pickupLocation?.Address ?? string.Empty;
        order.DeliveryLocationAddress = deliveryLocation?.Address ?? string.Empty;
        order.PickupAddress = order.PickupLocationAddress;
        order.DeliveryAddress = order.DeliveryLocationAddress;

        var logs = await _context.OrderStatusLogs
            .Where(l => l.OrderId == orderId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return (order, logs);
    }

    // ========================
    // GET DRIVER ORDERS
    // ========================
    public async Task<List<Order>> GetDriverOrdersAsync(int driverId)
    {
        var orderIds = await _context.Trips.Where(t => t.DriverId == driverId).Select(t => t.OrderId).ToListAsync();
        return await _context.Orders
            .Where(o => !o.IsDeleted && orderIds.Contains(o.Id))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    // ========================
    // GET AVAILABLE ORDERS
    // ========================
    public async Task<List<Order>> GetAvailableOrdersAsync()
    {
        var assignedOrderIds = await _context.Trips.Select(t => t.OrderId).Distinct().ToListAsync();
        return await _context.Orders
            .Where(o => !o.IsDeleted && o.Status == OrderStatus.Pending && !assignedOrderIds.Contains(o.Id))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    // ========================
    // CREATE
    // ========================


    public string GenerateOrderCode()
    {
        var bytes = Guid.NewGuid().ToByteArray();
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 12);
    }

    private async Task<int> GetOrCreateLocationAsync(string address)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Address == address);

        if (location != null)
            return location.Id;

        // Geocode the address
        var geocodeResult = await _geolocationService.GeocodeAddressAsync(address);
        if (!geocodeResult.Success || geocodeResult.Coordinates == null)
            throw new Exception($"Failed to geocode address: {address}");

        var newLocation = new Location
        {
            Name = address,
            Address = address,
            Lat = geocodeResult.Coordinates.Latitude,
            Lng = geocodeResult.Coordinates.Longitude,
            Type = LocationType.CustomerPoint
        };

        _context.Locations.Add(newLocation);
        await _context.SaveChangesAsync();

        // Ensure the ID is populated after save
        if (newLocation.Id <= 0)
            throw new Exception("Failed to generate Location ID");

        return newLocation.Id;
    }

    public async Task<bool> CreateOrderAsync(Order model, int userId)
    {
        // Validate that the customer (user) exists
        var customerExists = await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted);
        if (!customerExists)
            throw new Exception("Customer not found");

        model.CustomerId = userId;

        if (string.IsNullOrWhiteSpace(model.PickupAddress) || string.IsNullOrWhiteSpace(model.DeliveryAddress))
            throw new Exception("Pickup and delivery addresses are required");

        model.PickupLocId = await GetOrCreateLocationAsync(model.PickupAddress);
        model.DeliveryLocId = await GetOrCreateLocationAsync(model.DeliveryAddress);

        model.OrderCode = GenerateOrderCode();
        model.Status = OrderStatus.Pending;

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        model.IsDeleted = false;

        if (model.PickupLocId <= 0 || model.DeliveryLocId <= 0)
            throw new Exception("Invalid pickup/delivery location");

        var pickupLoc = await _context.Locations.FindAsync(model.PickupLocId);
        var deliveryLoc = await _context.Locations.FindAsync(model.DeliveryLocId);

        if (pickupLoc != null && deliveryLoc != null)
        {
            var distanceResult = await _geolocationService.GetDistanceAsync(
                new GeoPoint { Latitude = pickupLoc.Lat, Longitude = pickupLoc.Lng },
                new GeoPoint { Latitude = deliveryLoc.Lat, Longitude = deliveryLoc.Lng }
            );

            if (distanceResult.Success)
            {
                model.DistanceKm = distanceResult.DistanceMeters / 1000m;
            }
        }

        model.TotalPrice = CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);

        _context.Orders.Add(model);
        await _context.SaveChangesAsync();

        _context.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = model.Id,
            OldStatus = "",
            NewStatus = OrderStatus.Pending,
            ChangedByUserId = userId,
            Timestamp = DateTime.UtcNow,
            Note = "Created"
        });

        await _context.SaveChangesAsync();

        return true;
    }

    // ========================
    // UPDATE
    // ========================
    public async Task<bool> UpdateOrderAsync(Order model, int userId)
    {
        var order = await _context.Orders.FindAsync(model.Id);

        if (order == null || order.IsDeleted)
            return false;

        if (order.CustomerId != userId)
            return false;

        // Allow updates for orders that are not completed or cancelled
        if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
            return false;

        // Update pickup address if status is Pending or Picking
        if ((order.Status == OrderStatus.Pending || order.Status == OrderStatus.Picking) && 
            !string.IsNullOrWhiteSpace(model.PickupAddress))
        {
            order.PickupLocId = await GetOrCreateLocationAsync(model.PickupAddress);
        }

        // Update delivery address if status is NOT InTransit, Delivered, or Cancelled
        if (order.Status != OrderStatus.InTransit && 
            order.Status != OrderStatus.Delivered && 
            order.Status != OrderStatus.Cancelled && 
            !string.IsNullOrWhiteSpace(model.DeliveryAddress))
        {
            order.DeliveryLocId = await GetOrCreateLocationAsync(model.DeliveryAddress);
        }

        order.CargoType = model.CargoType;
        order.Weight = model.Weight;
        order.DistanceKm = model.DistanceKm;
        order.ScheduledPickupTime = model.ScheduledPickupTime;
        order.UpdatedAt = DateTime.UtcNow;
        order.TotalPrice = CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);

        await _context.SaveChangesAsync();
        return true;
    }

    // ========================
    // CANCEL
    // ========================
    public async Task<bool> CancelOrderAsync(int id, int userId, string reason)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null || order.IsDeleted)
            return false;

        if (order.CustomerId != userId)
            return false;

        if (order.Status == OrderStatus.Delivered)
            return false;

        var oldStatus = order.Status;

        order.Status = OrderStatus.Cancelled;
        order.CancelledBy = userId;
        order.CancelledReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        _context.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = id,
            OldStatus = oldStatus,
            NewStatus = OrderStatus.Cancelled,
            ChangedByUserId = userId,
            Timestamp = DateTime.UtcNow,
            Note = reason
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public decimal CalculatePrice(decimal distanceKm, string cargoType, decimal weight)
{
    decimal basePricePerKm = 10000;   
    decimal weightRate = 2000;

    decimal cargoMultiplier = cargoType switch
    {
        "Heavy" => 1.5m,
        "Fragile" => 1.2m,
        _ => 1.0m
    };

    return (distanceKm * basePricePerKm + weight * weightRate) * cargoMultiplier;
}

    // ========================
    // ASSIGN ORDER TO DRIVER
    // ========================
    public async Task<bool> AssignOrderToDriverAsync(int orderId, int driverId, int assignedByUserId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null || order.IsDeleted)
            return false;

        var isAssigned = await _context.Trips.AnyAsync(t => t.OrderId == orderId);
        if (order.Status != OrderStatus.Pending || isAssigned)
            return false;

        var driver = await _context.Drivers.FindAsync(driverId);
        var truckId = driver?.CurrentTruckId ?? 0;

        var oldStatus = order.Status;
        order.Status = OrderStatus.Assigned;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Trips.Add(new Trip
        {
            OrderId = orderId,
            DriverId = driverId,
            TruckId = truckId,
            StartTime = DateTime.UtcNow
        });

        _context.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = OrderStatus.Assigned,
            ChangedByUserId = assignedByUserId,
            Timestamp = DateTime.UtcNow,
            Note = $"Assigned to driver {driverId}"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    // ========================
    // UPDATE ORDER STATUS
    // ========================
    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, int changedByUserId, string note)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null || order.IsDeleted)
            return false;

        var oldStatus = order.Status;

        // Update status-specific fields
        if (newStatus == OrderStatus.InTransit)
        {
            order.ActualPickupTime = DateTime.UtcNow;
        }
        else if (newStatus == OrderStatus.Delivered)
        {
            order.ActualDeliveryTime = DateTime.UtcNow;
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        _context.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = orderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            Timestamp = DateTime.UtcNow,
            Note = note
        });

        await _context.SaveChangesAsync();
        return true;
    }
}