namespace TruckGrab.web.Models;

public enum TruckStatus
{
    Available,
    OnDelivery,
    Maintenance
}

public class Truck
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int TruckTypeId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty; 
    public TruckStatus Status { get; set; }
    public decimal CurrentLat { get; set; }
    public decimal CurrentLng { get; set; }
    public bool IsDeleted { get; set; }
}