namespace TruckGrab.web.Models;

public enum Role
{
    Admin,
    Driver,
    Customer
}
public class User
{
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Role Role { get; set; }
        public bool isActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserProfile Profile { get; set; } = new();
        public Driver Driver { get; set; } = new();

}
