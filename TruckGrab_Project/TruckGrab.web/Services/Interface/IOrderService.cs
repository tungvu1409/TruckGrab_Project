using TruckGrab.web.Models;

namespace TruckGrab.web.Services.Interface;

public interface IOrderService
{
    // ========================
    // QUERY
    // ========================
    List<Order> GetOrders(int userId);
    (Order? Order, List<OrderStatusLog> Logs) GetOrderDetail(int orderId, int userId);

    // ========================
    // COMMAND
    // ========================
    bool CreateOrder(Order order, int userId);
    bool UpdateOrder(Order order, int userId);
    bool CancelOrder(int orderId, int userId, string reason);

    // ========================
    // BUSINESS
    // ========================
    decimal CalculatePrice(decimal distanceKm, string cargoType, decimal weight);
}