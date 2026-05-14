namespace TruckGrab.web.Models;
public class UserProfile
{
    public int UserId { get; set; }
    public User? User { get; set; }    
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }

}