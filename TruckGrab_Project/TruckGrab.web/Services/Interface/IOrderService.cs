using TruckGrab.web.Models;

namespace TruckGrab.web.Services.Interface;

public interface IOrderService
{
    // ========================
    // QUERY
    // ========================
    List<Order> GetOrders(int userId);
    (Order? Order, List<OrderStatusLog> Logs) GetOrderDetail(int orderId, int userId);
    List<Order> GetDriverOrders(int driverId);
    (Order? Order, List<OrderStatusLog> Logs) GetDriverOrderDetail(int orderId, int driverId);
    List<Order> GetAvailableOrders();

    // ========================
    // COMMAND
    // ========================
    bool CreateOrder(Order order, int userId);
    bool UpdateOrder(Order order, int userId);
    bool CancelOrder(int orderId, int userId, string reason);
    bool AssignOrderToDriver(int orderId, int driverId, int assignedByUserId);
    bool UpdateOrderStatus(int orderId, string newStatus, int changedByUserId, string note);

    // ========================
    // BUSINESS
    // ========================
    decimal CalculatePrice(decimal distanceKm, string cargoType, decimal weight);
}