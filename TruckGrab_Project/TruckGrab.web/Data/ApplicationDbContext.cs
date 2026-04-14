namespace TruckGrab.web.Data;

using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<DriverRating> DriverRatings { get; set; }
    public DbSet<FuelLog> FuelLogs { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderStatusLog> OrderStatusLogs { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Truck> Trucks { get; set; }
    public DbSet<TruckDriverAssignment> TruckDriverAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.Property(u => u.Id)
		    .HasColumnName("id"); 

            entity.Property(u => u.UserName)
		    .HasColumnName("username"); 

            entity.Property(u => u.PasswordHash)
		    .HasColumnName("password_hash"); 

            entity.Property(u => u.Role)
		    .HasColumnName("role")
            .HasConversion<string>(); 

            entity.Property(u => u.IsActive)
		    .HasColumnName("is_active"); 

            entity.Property(u => u.IsDeleted)
		    .HasColumnName("is_deleted"); 

            entity.Property(u => u.CreatedAt)
            .HasColumnName("created_at");

            entity.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

            entity.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");

            entity.HasKey(p => p.UserId);

            entity.Property(p => p.UserId)
            .HasColumnName("user_id");
            entity.Property(p => p.FullName)
            .HasColumnName("full_name");
            entity.Property(p => p.Email)
            .HasColumnName("email");
            entity.Property(p => p.Phone)
            .HasColumnName("phone");
            entity.Property(p => p.AvatarUrl)
            .HasColumnName("avatar_url");
            entity.Property(p => p.Address)
            .HasColumnName("address");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("drivers");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
            .HasColumnName("id");
            entity.Property(d => d.UserId)
            .HasColumnName("user_id");

            entity.Property(d => d.LicenseNumber)   
            .HasColumnName("license_number");
            entity.Property(d => d.LicenseClass)
            .HasColumnName("license_class");
            entity.Property(d => d.ExperienceYears)
            .HasColumnName("experience_years");
            entity.Property(d => d.RatingAvg)
            .HasColumnName("rating_avg");
            entity.Property(d => d.Status)
            .HasColumnName("status");
            entity.Property(d => d.CurrentTruckId)
            .HasColumnName("current_truck_id");
            entity.Property(d => d.IsDeleted)
            .HasColumnName("is_deleted");
        });
        
    }
}