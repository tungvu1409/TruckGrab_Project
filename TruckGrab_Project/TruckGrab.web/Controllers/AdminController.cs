using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Data;
using TruckGrab.web.Models;
using TruckGrab.web.Services;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Controllers;

[Route("Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly DriverLocationStore _locationStore;
    private readonly IGeolocationService _geolocationService;
    private readonly IOrderService _orderService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context, 
        DriverLocationStore locationStore, 
        IGeolocationService geolocationService,
        IOrderService orderService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _locationStore = locationStore;
        _geolocationService = geolocationService;
        _orderService = orderService;
        _logger = logger;
    }

    private IActionResult CheckAdmin()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account");

        if (!User.IsInRole("Admin"))
            return Forbid();

        return null!;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return View();
    }

    [HttpGet("GetStats")]
    public IActionResult GetStats()
    {
        var auth = CheckAdmin();
        if (auth != null) return Unauthorized();

        var totalUsers = _context.Users.Count(u => !u.IsDeleted);
        var totalDrivers = _context.Drivers.Count(d => !d.IsDeleted);
        var totalOrders = _context.Orders.Count(o => !o.IsDeleted);
        var totalRevenue = _context.Orders.Where(o => !o.IsDeleted && o.Status == "Delivered").Sum(o => o.TotalPrice);

        return Json(new
        {
            totalUsers,
            totalDrivers,
            totalOrders,
            totalRevenue
        });
    }

    [HttpGet("GetRecentActivity")]
    public IActionResult GetRecentActivity()
    {
        var auth = CheckAdmin();
        if (auth != null) return Unauthorized();

        var recentOrders = _context.Orders
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new { o.OrderCode, o.Status, CreatedAt = o.CreatedAt.ToString("g") })
            .ToList();

        var availableDrivers = _context.Drivers
            .Where(d => !d.IsDeleted && d.Status == DriverStatus.Active)
            .OrderByDescending(d => d.Id)
            .Take(5)
            .Select(d => new { 
                Name = _context.Users.Where(u => u.Id == d.UserId).Select(u => u.UserName).FirstOrDefault() ?? "Unknown",
                Status = d.Status.ToString(),
                Rating = d.RatingAvg
            })
            .ToList();

        return Json(new { recentOrders, availableDrivers });
    }

    [HttpGet("Manage/Users")]
    public IActionResult ManageUsers(string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var usersQuery = _context.Users
            .Where(u => !u.IsDeleted)
            .Include(u => u.Profile)
            .AsQueryable();

        IQueryable<User> orderedQuery = sortBy.ToLower() switch
        {
            "id"         => sortOrder == "asc" ? usersQuery.OrderBy(u => u.Id)         : usersQuery.OrderByDescending(u => u.Id),
            "username"   => sortOrder == "asc" ? usersQuery.OrderBy(u => u.UserName)    : usersQuery.OrderByDescending(u => u.UserName),
            "email"      => sortOrder == "asc" ? usersQuery.OrderBy(u => u.Profile.Email)  : usersQuery.OrderByDescending(u => u.Profile.Email),
            "phone"      => sortOrder == "asc" ? usersQuery.OrderBy(u => u.Profile.Phone)  : usersQuery.OrderByDescending(u => u.Profile.Phone),
            "status"     => sortOrder == "asc" ? usersQuery.OrderBy(u => u.IsActive)    : usersQuery.OrderByDescending(u => u.IsActive),
            _            => sortOrder == "asc" ? usersQuery.OrderBy(u => u.CreatedAt)   : usersQuery.OrderByDescending(u => u.CreatedAt),
        };

        ViewBag.SortBy        = sortBy;
        ViewBag.SortOrder     = sortOrder;
        ViewBag.NextSortOrder = sortOrder == "asc" ? "desc" : "asc";

        return View(orderedQuery.ToList());
    }

    [HttpGet("Manage/Users/Details/{id}")]
    public IActionResult UserDetails(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Driver)
            .FirstOrDefault(u => u.Id == id && !u.IsDeleted);

        if (user == null) return NotFound();

        return View("Details", user);
    }

    [HttpPost("Manage/Users/Toggle/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleUserStatus(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        user.IsActive  = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        return RedirectToAction("ManageUsers");
    }

    [HttpPost("Manage/Users/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteUser(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        return RedirectToAction("ManageUsers");
    }

    [HttpGet("Manage/Drivers")]
    public IActionResult ManageDrivers(string sortBy = "Id", string sortOrder = "asc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var driversQuery = _context.Drivers
            .Where(d => !d.IsDeleted);

        IQueryable<Driver> orderedQuery = sortBy.ToLower() switch
        {
            "licensenumber" => sortOrder == "asc" ? driversQuery.OrderBy(d => d.LicenseNumber)    : driversQuery.OrderByDescending(d => d.LicenseNumber),
            "status"        => sortOrder == "asc" ? driversQuery.OrderBy(d => d.Status)            : driversQuery.OrderByDescending(d => d.Status),
            _               => sortOrder == "asc" ? driversQuery.OrderBy(d => d.Id)                : driversQuery.OrderByDescending(d => d.Id),
        };

        var drivers = orderedQuery.ToList();
        var userIds = drivers.Select(d => d.UserId).ToList();
        var users   = _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Include(u => u.Profile)
            .ToDictionary(u => u.Id);

        var trucks  = _context.Trucks.ToDictionary(t => t.Id);

        ViewBag.Users         = users;
        ViewBag.Trucks        = trucks;
        ViewBag.SortBy        = sortBy;
        ViewBag.SortOrder     = sortOrder;
        ViewBag.NextSortOrder = sortOrder == "asc" ? "desc" : "asc";

        return View(drivers);
    }

    [HttpGet("Manage/Drivers/AssignTruck/{id}")]
    public async Task<IActionResult> AssignTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null) return NotFound();

        var trucks = await _context.Trucks.Where(t => !t.IsDeleted && (t.Status == TruckStatus.Available || t.Id == driver.CurrentTruckId)).ToListAsync();
        
        var user = await _context.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == driver.UserId);

        ViewBag.DriverName = user?.Profile?.FullName ?? user?.UserName ?? "Driver #" + driver.Id;
        ViewBag.Trucks = trucks;

        return View(driver);
    }

    [HttpPost("Manage/Drivers/AssignTruck/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTruck(int id, int truckId)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null) return NotFound();

        if (driver.CurrentTruckId.HasValue && driver.CurrentTruckId != truckId)
        {
            var oldTruck = await _context.Trucks.FindAsync(driver.CurrentTruckId.Value);
            if (oldTruck != null) oldTruck.Status = TruckStatus.Available;
        }

        if (truckId > 0)
        {
            driver.CurrentTruckId = truckId;
            var newTruck = await _context.Trucks.FindAsync(truckId);
            if (newTruck != null) newTruck.Status = TruckStatus.OnTrip;
        }
        else
        {
            driver.CurrentTruckId = null;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cập nhật gán xe thành công!";
        return RedirectToAction("ManageDrivers");
    }

    [HttpGet("Manage/Orders")]
    public IActionResult ManageOrders(string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var ordersQuery = _context.Orders
            .Where(o => !o.IsDeleted);

        IQueryable<Order> orderedQuery = sortBy.ToLower() switch
        {
            "ordercode"       => sortOrder == "asc" ? ordersQuery.OrderBy(o => o.OrderCode)                 : ordersQuery.OrderByDescending(o => o.OrderCode),
            "status"          => sortOrder == "asc" ? ordersQuery.OrderBy(o => o.Status)                    : ordersQuery.OrderByDescending(o => o.Status),
            "pickupaddress"   => sortOrder == "asc" ? ordersQuery.OrderBy(o => o.PickupLocationAddress)     : ordersQuery.OrderByDescending(o => o.PickupLocationAddress),
            "deliveryaddress" => sortOrder == "asc" ? ordersQuery.OrderBy(o => o.DeliveryLocationAddress)   : ordersQuery.OrderByDescending(o => o.DeliveryLocationAddress),
            _                 => sortOrder == "asc" ? ordersQuery.OrderBy(o => o.CreatedAt)                 : ordersQuery.OrderByDescending(o => o.CreatedAt),
        };

        ViewBag.SortBy        = sortBy;
        ViewBag.SortOrder     = sortOrder;
        ViewBag.NextSortOrder = sortOrder == "asc" ? "desc" : "asc";

        return View(orderedQuery.ToList());
    }


    [HttpGet("LiveMap")]
    public IActionResult LiveMap()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return View();
    }

    [HttpGet("LiveDriverLocations")]
    public IActionResult LiveDriverLocations()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var drivers = _locationStore.GetOnlineDrivers(maxAgeMinutes: 2)
            .Select(l => new
            {
                driverId  = l.DriverId,
                name      = l.Name,
                lat       = l.Lat,
                lng       = l.Lng,
                status    = l.Status,
                updatedAt = l.UpdatedAt
            }).ToList();

        return Json(drivers);
    }

    [HttpGet("Manage/Orders/Assign/{id}")]
    public async Task<IActionResult> AssignOrder(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var order = await _context.Orders.FindAsync(id);
        if (order == null || order.IsDeleted) return NotFound();

        if (order.Status != "pending")
        {
            TempData["ErrorMessage"] = "Only pending orders can be assigned.";
            return RedirectToAction("ManageOrders");
        }

        var pickupLoc = await _context.Locations.FindAsync(order.PickupLocId);
        if (pickupLoc == null) return BadRequest("Order has no valid pickup location.");

        var orderPoint = new GeoPoint { Latitude = pickupLoc.Lat, Longitude = pickupLoc.Lng };

        var onlineDrivers = _locationStore.GetOnlineDrivers(maxAgeMinutes: 30); 

        var recommendedDrivers = onlineDrivers
            .Select(d => new
            {
                Driver = d,
                Distance = _geolocationService.CalculateHaversineDistance(
                    orderPoint, 
                    new GeoPoint { Latitude = (decimal)d.Lat, Longitude = (decimal)d.Lng })
            })
            .OrderBy(x => x.Distance)
            .Take(3)
            .Select(x => new RecommendedDriverViewModel
            {
                DriverId = x.Driver.DriverId,
                Name = x.Driver.Name,
                DistanceKm = x.Distance,
                Status = x.Driver.Status,
                Lat = x.Driver.Lat,
                Lng = x.Driver.Lng
            })
            .ToList();

        ViewBag.RecommendedDrivers = recommendedDrivers;
        ViewBag.PickupAddress = pickupLoc.Address;

        return View(order);
    }

    [HttpPost("Manage/Orders/Assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitAssignment(int orderId, int driverId)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var success = await _orderService.AssignOrderToDriverAsync(orderId, driverId, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Order assigned successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to assign order. It might have been already assigned or cancelled.";
        }

        return RedirectToAction("ManageOrders");
    }

    // ===== TRUCK MANAGEMENT =====

    [HttpGet("ManageTrucks")]
    public async Task<IActionResult> ManageTrucks()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var trucks = await _context.Trucks.Where(t => !t.IsDeleted).ToListAsync();
        return View(trucks);
    }

    [HttpGet("CreateTruck")]
    public async Task<IActionResult> CreateTruck()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        ViewBag.TruckTypes = await _context.TruckTypes.ToListAsync();
        return View(new Truck());
    }

    [HttpPost("CreateTruck")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTruck(Truck truck)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        if (ModelState.IsValid)
        {
            _context.Trucks.Add(truck);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm xe tải thành công!";
            return RedirectToAction("ManageTrucks");
        }
        ViewBag.TruckTypes = await _context.TruckTypes.ToListAsync();
        return View(truck);
    }

    [HttpGet("EditTruck/{id}")]
    public async Task<IActionResult> EditTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var truck = await _context.Trucks.FindAsync(id);
        if (truck == null) return NotFound();

        ViewBag.TruckTypes = await _context.TruckTypes.ToListAsync();
        return View(truck);
    }

    [HttpPost("EditTruck/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTruck(int id, Truck truck)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var existingTruck = await _context.Trucks.FindAsync(id);
        if (existingTruck == null) return NotFound();

        if (ModelState.IsValid)
        {
            existingTruck.LicensePlate = truck.LicensePlate;
            existingTruck.TruckTypeId = truck.TruckTypeId;
            existingTruck.Brand = truck.Brand;
            existingTruck.Model = truck.Model;
            existingTruck.FuelType = truck.FuelType;
            existingTruck.Status = truck.Status;
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật xe tải thành công!";
            return RedirectToAction("ManageTrucks");
        }
        ViewBag.TruckTypes = await _context.TruckTypes.ToListAsync();
        return View(truck);
    }

    [HttpPost("DeleteTruck/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var truck = await _context.Trucks.FindAsync(id);
        if (truck != null)
        {
            truck.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa xe tải thành công!";
        }
        return RedirectToAction("ManageTrucks");
    }

    [HttpGet("Profile")]
    public IActionResult Profile()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return RedirectToAction("Profile", "Account");
    }
}

public class RecommendedDriverViewModel
{
    public int DriverId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
}