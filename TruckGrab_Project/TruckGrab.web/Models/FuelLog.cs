public class FuelLog
{
    public int Id { get; set; }
    public int TruckId { get; set; }
    public int DriverId { get; set; }
    public DateTime RefuelDate { get; set; }
    public decimal Liters { get; set; }
    public decimal CostAmount { get; set; }
}