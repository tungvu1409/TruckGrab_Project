public class OrderStatusLog
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedByUserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Note { get; set; } = string.Empty;
}