using TruckGrab.web.Models;

namespace TruckGrab.web.Services.Interface;

public interface IOrderService
{
    // ========================
    // QUERY
    // ========================
    Task<List<Order>> GetOrdersAsync(int userId);
    Task<(Order? Order, List<OrderStatusLog> Logs)> GetOrderDetailAsync(int orderId, int userId);
    Task<List<Order>> GetDriverOrdersAsync(int driverId);
    Task<(Order? Order, List<OrderStatusLog> Logs)> GetDriverOrderDetailAsync(int orderId, int driverId);
    Task<List<Order>> GetAvailableOrdersAsync();

    // ========================
    // COMMAND
    // ========================
    Task<bool> CreateOrderAsync(Order order, int userId);
    Task<bool> UpdateOrderAsync(Order order, int userId);
    Task<bool> CancelOrderAsync(int orderId, int userId, string reason);
    Task<bool> AssignOrderToDriverAsync(int orderId, int driverId, int assignedByUserId);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, int changedByUserId, string note);

    // ========================
    // BUSINESS
    // ========================
    decimal CalculatePrice(decimal distanceKm, string cargoType, decimal weight);
}