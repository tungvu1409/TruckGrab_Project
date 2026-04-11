public class DriverRating
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int DriverId { get; set; }
    public int Score { get; set; } 
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}