namespace TruckGrab.web.Models;
public enum LocationType
{
    Warehouse,
    Customer,
    ServiceStation
}
public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public LocationType Type { get; set; }
}