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
        public const string Pending = "Pending";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
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

        var logs = _context.OrderStatusLogs
            .Where(l => l.OrderId == id)
            .OrderByDescending(l => l.Timestamp)
            .ToList();

        return (order, logs);
    }

    // ========================
    // CREATE
    // ========================
    public bool CreateOrder(Order model, int userId)
    {
        model.CustomerId = userId;
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        model.Status = OrderStatus.Pending;
        model.OrderCode = $"ORD-{DateTime.UtcNow.Ticks}";

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

        if (order.Status != OrderStatus.Pending)
            return false;

        order.CargoType = model.CargoType;
        order.Weight = model.Weight;
        order.DistanceKm = model.DistanceKm;
        order.PickupLocId = model.PickupLocId;
        order.DeliveryLocId = model.DeliveryLocId;
        order.ScheduledPickupTime = model.ScheduledPickupTime;
        order.UpdatedAt = DateTime.UtcNow;

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

        if (order.Status == OrderStatus.Completed)
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