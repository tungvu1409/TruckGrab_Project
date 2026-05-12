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

    // ========================
    // MANAGE USERS  →  /Admin/Manage/Users
    // ========================

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

    // ========================
    // MANAGE DRIVERS  →  /Admin/Manage/Drivers
    // ========================

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

        ViewBag.Users         = users;
        ViewBag.SortBy        = sortBy;
        ViewBag.SortOrder     = sortOrder;
        ViewBag.NextSortOrder = sortOrder == "asc" ? "desc" : "asc";

        return View(drivers);
    }

    // ========================
    // MANAGE ORDERS  →  /Admin/Manage/Orders
    // ========================

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

    // ========================
    // TRUCKS  →  /Admin/Manage/Trucks
    // ========================

    [HttpGet("Manage/Trucks")]
    public IActionResult Trucks()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var trucks = _context.Trucks
            .Where(t => !t.IsDeleted)
            .ToList();

        return View(trucks);
    }

    [HttpGet("Manage/Trucks/Create")]
    public IActionResult CreateTruck()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return View();
    }

    [HttpPost("Manage/Trucks/Create")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateTruck(Truck truck)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View(truck);

        _context.Trucks.Add(truck);
        _context.SaveChanges();

        return RedirectToAction("Trucks");
    }

    [HttpGet("Manage/Trucks/Edit/{id}")]
    public IActionResult EditTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var truck = _context.Trucks.Find(id);
        if (truck == null) return NotFound();

        return View(truck);
    }

    [HttpPost("Manage/Trucks/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult EditTruck(Truck truck)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View(truck);

        _context.Trucks.Update(truck);
        _context.SaveChanges();

        return RedirectToAction("Trucks");
    }

    [HttpGet("Manage/Trucks/Delete/{id}")]
    public IActionResult DeleteTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var truck = _context.Trucks.Find(id);
        if (truck == null) return NotFound();

        truck.IsDeleted = true;
        _context.SaveChanges();

        return RedirectToAction("Trucks");
    }

    // ========================
    // LIVE MAP  →  /Admin/LiveMap
    // ========================

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

        // Read from in-memory store
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

        _logger.LogInformation("[GPS] Live map requested. Found {Count} online drivers.", drivers.Count);

        return Json(drivers);
    }

    // ========================
    // ORDER ASSIGNMENT
    // ========================

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

        // Get pickup location coordinates
        var pickupLoc = await _context.Locations.FindAsync(order.PickupLocId);
        if (pickupLoc == null) return BadRequest("Order has no valid pickup location.");

        var orderPoint = new GeoPoint { Latitude = pickupLoc.Lat, Longitude = pickupLoc.Lng };

        // Get all online drivers
        var onlineDrivers = _locationStore.GetOnlineDrivers(maxAgeMinutes: 30); // 30 mins for demo

        // Calculate distances
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