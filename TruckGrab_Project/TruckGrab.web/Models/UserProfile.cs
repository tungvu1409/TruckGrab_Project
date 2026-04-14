namespace TruckGrab.web.Models;
public class UserProfile
{
    public int UserId { get; set; }
    public User User { get; set; } = new();    
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

}