using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Data;
using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Implementation;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
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
    public List<Order> GetOrders(int userId)
    {
        return _context.Orders
            .Where(o => !o.IsDeleted && o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();
    }

    // ========================
    // GET DETAIL
    // ========================
    public (Order?, List<OrderStatusLog>) GetOrderDetail(int id, int userId)
    {
        var order = _context.Orders
            .FirstOrDefault(o => o.Id == id && !o.IsDeleted);

        if (order == null || order.CustomerId != userId)
            return (null, new List<OrderStatusLog>());

        var pickupLocation = _context.Locations
            .FirstOrDefault(l => l.Id == order.PickupLocId);

        var deliveryLocation = _context.Locations
            .FirstOrDefault(l => l.Id == order.DeliveryLocId);

        order.PickupLocationAddress = pickupLocation?.Address ?? string.Empty;
        order.DeliveryLocationAddress = deliveryLocation?.Address ?? string.Empty;
        order.PickupAddress = order.PickupLocationAddress;
        order.DeliveryAddress = order.DeliveryLocationAddress;

        var logs = _context.OrderStatusLogs
            .Where(l => l.OrderId == id)
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        return (order, logs);
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

    private int GetOrCreateLocation(string address)
    {
        var location = _context.Locations
            .FirstOrDefault(l => l.Address == address);

        if (location != null)
            return location.Id;

        var newLocation = new Location
        {
            Name = address,
            Address = address,
            Lat = 0,
            Lng = 0,
            Type = LocationType.CustomerPoint
        };

        _context.Locations.Add(newLocation);
        _context.SaveChanges();

        // Ensure the ID is populated after save
        if (newLocation.Id <= 0)
            throw new Exception("Failed to generate Location ID");

        return newLocation.Id;
    }

    public bool CreateOrder(Order model, int userId)
    {
        // Validate that the customer (user) exists
        var customerExists = _context.Users.Any(u => u.Id == userId && !u.IsDeleted);
        if (!customerExists)
            throw new Exception("Customer not found");

        model.CustomerId = userId;

        if (string.IsNullOrWhiteSpace(model.PickupAddress) || string.IsNullOrWhiteSpace(model.DeliveryAddress))
            throw new Exception("Pickup and delivery addresses are required");

        model.PickupLocId = GetOrCreateLocation(model.PickupAddress);
        model.DeliveryLocId = GetOrCreateLocation(model.DeliveryAddress);

        model.OrderCode = GenerateOrderCode();
        model.Status = OrderStatus.Pending;

        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        model.IsDeleted = false;

        if (model.PickupLocId <= 0 || model.DeliveryLocId <= 0)
            throw new Exception("Invalid pickup/delivery location");

        model.TotalPrice = CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);

        _context.Orders.Add(model);
        _context.SaveChanges();

        _context.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = model.Id,
            OldStatus = "",
            NewStatus = OrderStatus.Pending,
            ChangedByUserId = userId,
            Timestamp = DateTime.UtcNow,
            Note = "Created"
        });

        _context.SaveChanges();

        return true;
    }

    // ========================
    // UPDATE
    // ========================
    public bool UpdateOrder(Order model, int userId)
    {
        var order = _context.Orders.Find(model.Id);

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
            order.PickupLocId = GetOrCreateLocation(model.PickupAddress);
        }

        // Update delivery address if status is NOT InTransit, Delivered, or Cancelled
        if (order.Status != OrderStatus.InTransit && 
            order.Status != OrderStatus.Delivered && 
            order.Status != OrderStatus.Cancelled && 
            !string.IsNullOrWhiteSpace(model.DeliveryAddress))
        {
            order.DeliveryLocId = GetOrCreateLocation(model.DeliveryAddress);
        }

        order.CargoType = model.CargoType;
        order.Weight = model.Weight;
        order.DistanceKm = model.DistanceKm;
        order.ScheduledPickupTime = model.ScheduledPickupTime;
        order.UpdatedAt = DateTime.UtcNow;
        order.TotalPrice = CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);

        _context.SaveChanges();
        return true;
    }

    // ========================
    // CANCEL
    // ========================
    public bool CancelOrder(int id, int userId, string reason)
    {
        var order = _context.Orders.Find(id);

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

        _context.SaveChanges();
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
}