namespace TruckGrab.web.Models;


public enum DriverStatus
{
    Available,
    OnDelivery,
    OffDuty
}

public class Driver
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public decimal RatingAvg { get; set; }
    public DriverStatus Status { get; set; } 
    public int? CurrentTruckId { get; set; }
    public bool IsDeleted { get; set; }
}