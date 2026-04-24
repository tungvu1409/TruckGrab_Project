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
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(d => d.CurrentTruckId)
                .HasColumnName("current_truck_id");
            entity.Property(d => d.IsDeleted)
                .HasColumnName("is_deleted");
        });

        modelBuilder.Entity<DriverRating>(entity =>
        {
            entity.ToTable("driver_ratings");

            entity.HasKey(d => d.Id);

            entity.Property(d => d.Id)
                .HasColumnName("id");
            entity.Property(d => d.OrderId)
                .HasColumnName("order_id");
            entity.Property(d => d.CustomerId)
                .HasColumnName("customer_id");
            entity.Property(d => d.DriverId)
                .HasColumnName("driver_id");
            entity.Property(d => d.Score)
                .HasColumnName("score");
            entity.Property(d => d.Comment)
                .HasColumnName("comment");
            entity.Property(d => d.CreatedAt)
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<FuelLog>(entity =>
        {
            entity.ToTable("fuel_logs");

            entity.HasKey(f => f.Id);

            entity.Property(f => f.Id)
                .HasColumnName("id");
            entity.Property(f => f.TruckId)
                .HasColumnName("truck_id");
            entity.Property(f => f.DriverId)
                .HasColumnName("driver_id");
            entity.Property(f => f.RefuelDate)
                .HasColumnName("refuel_date");
            entity.Property(f => f.Liters)
                .HasColumnName("liters");
            entity.Property(f => f.CostAmount)
                .HasColumnName("cost_amount");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");

            entity.HasKey(l => l.Id);

            entity.Property(l => l.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(l => l.Name)
                .HasColumnName("name");
            entity.Property(l => l.Address)
                .HasColumnName("address");
            entity.Property(l => l.Lat)
                .HasColumnName("lat");
            entity.Property(l => l.Lng)
                .HasColumnName("lng");
            entity.Property(l => l.Type)
                .HasColumnName("type")
                .HasConversion(
                v => v == LocationType.Warehouse ? "warehouse" : "customer_point",
                v => v == "warehouse"
                    ? LocationType.Warehouse
                    : LocationType.CustomerPoint
            );
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.Id)
                .HasColumnName("id");
            entity.Property(o => o.OrderCode)
                .HasColumnName("order_code");
            entity.Property(o => o.CustomerId)
                .HasColumnName("customer_id");
            entity.Property(o => o.DriverId)
                .HasColumnName("driver_id");
            entity.Property(o => o.PickupLocId)
                .HasColumnName("pickup_loc_id");
            entity.Property(o => o.DeliveryLocId)
                .HasColumnName("delivery_loc_id");
            entity.Property(o => o.CargoType)
                .HasColumnName("cargo_type");
            entity.Property(o => o.Weight)
                .HasColumnName("weight");
            entity.Property(o => o.DistanceKm)
                .HasColumnName("distance_km");
            entity.Property(o => o.TotalPrice)
                .HasColumnName("total_price");
            entity.Property(o => o.Status)
                .HasColumnName("status");
            entity.Property(o => o.ScheduledPickupTime)
                .HasColumnName("scheduled_pickup_time");
            entity.Property(o => o.ActualPickupTime)
                .HasColumnName("actual_pickup_time");
            entity.Property(o => o.ActualDeliveryTime)
                .HasColumnName("actual_delivery_time");
            entity.Property(o => o.CancelledBy)
                .HasColumnName("cancelled_by");
            entity.Property(o => o.CancelledReason)
                .HasColumnName("cancelled_reason");
            entity.Property(o => o.IsDeleted)
                .HasColumnName("is_deleted");
            entity.Property(o => o.CreatedAt)
                .HasColumnName("created_at");
            entity.Property(o => o.UpdatedAt)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<OrderStatusLog>(entity =>
        {
            entity.ToTable("order_status_logs");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .HasColumnName("id");
            entity.Property(s => s.OrderId)
                .HasColumnName("order_id");
            entity.Property(s => s.OldStatus)
                .HasColumnName("old_status");
            entity.Property(s => s.NewStatus)
                .HasColumnName("new_status");
            entity.Property(s => s.ChangedByUserId)
                .HasColumnName("changed_by_user_id");
            entity.Property(s => s.Timestamp)
                .HasColumnName("timestamp");
            entity.Property(s => s.Note)
                .HasColumnName("note");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id");
            entity.Property(p => p.OrderId)
                .HasColumnName("order_id");
            entity.Property(p => p.Amount)
                .HasColumnName("amount");
            entity.Property(p => p.PaymentMethod)
                .HasColumnName("payment_method")
                .HasConversion<string>();
            entity.Property(p => p.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(p => p.TransactionCode)
                .HasColumnName("transaction_code");
            entity.Property(p => p.PaidAt)
                .HasColumnName("paid_at");
            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("trips");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .HasColumnName("id");
            entity.Property(t => t.OrderId)
                .HasColumnName("order_id");
            entity.Property(t => t.TruckId)
                .HasColumnName("truck_id");
            entity.Property(t => t.DriverId)
                .HasColumnName("driver_id");
            entity.Property(t => t.StartTime)
                .HasColumnName("start_time");
            entity.Property(t => t.EndTime)
                .HasColumnName("end_time");
            entity.Property(t => t.ActualRouteUrl)
                .HasColumnName("actual_route_url");
        });

        modelBuilder.Entity<Truck>(entity =>
        {
            entity.ToTable("trucks");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                .HasColumnName("id");
            entity.Property(t => t.LicensePlate)
                .HasColumnName("license_plate");
            entity.Property(t => t.TruckTypeId)
                .HasColumnName("truck_type_id");
            entity.Property(t => t.Brand)
                .HasColumnName("brand");
            entity.Property(t => t.Model)
                .HasColumnName("model");
            entity.Property(t => t.FuelType)
                .HasColumnName("fuel_type");
            entity.Property(t => t.Status)
                .HasColumnName("status")
                .HasConversion<string>();
            entity.Property(t => t.CurrentLat)
                .HasColumnName("current_lat");
            entity.Property(t => t.CurrentLng)
                .HasColumnName("current_lng");
            entity.Property(t => t.IsDeleted)
                .HasColumnName("is_deleted");
        });

        modelBuilder.Entity<TruckDriverAssignment>(entity =>
        {
            entity.ToTable("truck_driver_assignment");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id)
                .HasColumnName("id");
            entity.Property(a => a.TruckId)
                .HasColumnName("truck_id");
            entity.Property(a => a.DriverId)
                .HasColumnName("driver_id");
            entity.Property(a => a.AssignedAt)
                .HasColumnName("assigned_at");
            entity.Property(a => a.IsPrimary)
                .HasColumnName("is_primary");
        });
    }
}


