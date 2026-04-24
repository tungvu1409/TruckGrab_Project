using System.ComponentModel.DataAnnotations.Schema;



namespace TruckGrab.web.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? DriverId { get; set; } // Added DriverId field
    public int PickupLocId { get; set; }
    public int DeliveryLocId { get; set; }

    [NotMapped]
    public string PickupAddress { get; set; } = string.Empty;

    [NotMapped]
    public string DeliveryAddress { get; set; } = string.Empty;

    [NotMapped]
    public string PickupLocationAddress { get; set; } = string.Empty;

    [NotMapped]
    public string DeliveryLocationAddress { get; set; } = string.Empty;

    public string CargoType { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty; // ENUM
    public DateTime? ScheduledPickupTime { get; set; }
    public DateTime? ActualPickupTime { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public int? CancelledBy { get; set; }
    public string CancelledReason { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}