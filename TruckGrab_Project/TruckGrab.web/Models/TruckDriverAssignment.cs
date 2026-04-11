public class TruckDriverAssignment
{
    public int Id { get; set; }
    public int TruckId { get; set; }
    public int DriverId { get; set; }
    public DateTime AssignedAt { get; set; }
    public bool IsPrimary { get; set; }
}