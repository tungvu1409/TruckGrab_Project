public class Trip
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int TruckId { get; set; }
    public int DriverId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string ActualRouteUrl { get; set; } = string.Empty;
}