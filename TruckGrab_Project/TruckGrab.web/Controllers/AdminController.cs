using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Data;
using TruckGrab.web.Models;

namespace TruckGrab.web.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========================
    // AUTH CHECK
    // ========================
    private IActionResult CheckAdmin()
    {
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
            return RedirectToAction("Login", "Account");

        if (role != "Admin")
            return Forbid();

        return null!;
    }

    // ========================
    // DASHBOARD
    // ========================
    public IActionResult Index()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return View();
    }

    // ========================
    // USERS
    // ========================
    public IActionResult Users()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var users = _context.Users
            .Where(u => !u.IsDeleted)
            .ToList();

        return View(users);
    }

    public IActionResult ToggleUserStatus(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return RedirectToAction("Users");
    }

    public IActionResult DeleteUser(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users.Find(id);
        if (user == null) return NotFound();

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();

        return RedirectToAction("Users");
    }

    // ===== USER DETAILS =====
    public IActionResult Details(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var user = _context.Users
            .Include(u => u.Profile)
            .Include(u => u.Driver)
            .FirstOrDefault(u => u.Id == id && !u.IsDeleted);

        if (user == null) return NotFound();

        return View(user);
    }

    // ========================
    // DRIVERS
    // ========================
    public IActionResult Drivers()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var drivers = _context.Drivers
            .Where(d => !d.IsDeleted)
            .ToList();

        return View(drivers);
    }

    // ========================
    // ORDERS
    // ========================
    public IActionResult Orders()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var orders = _context.Orders
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        return View(orders);
    }

    // ========================
    // TRUCKS
    // ========================
    public IActionResult Trucks()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var trucks = _context.Trucks
            .Where(t => !t.IsDeleted)
            .ToList();

        return View(trucks);
    }

    // ===== CREATE TRUCK =====
    [HttpGet]
    public IActionResult CreateTruck()
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        return View();
    }

    [HttpPost]
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

    // ===== EDIT TRUCK =====
    [HttpGet]
    public IActionResult EditTruck(int id)
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var truck = _context.Trucks.Find(id);
        if (truck == null) return NotFound();

        return View(truck);
    }

    [HttpPost]
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

    // ===== DELETE TRUCK =====
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
    // MANAGE SECTION
    // ========================

    // ===== MANAGE ORDERS =====
    public IActionResult ManageOrders(string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var ordersQuery = _context.Orders
            .Where(o => !o.IsDeleted);

        // Apply sorting
        ordersQuery = sortBy.ToLower() switch
        {
            "ordercode" => sortOrder.ToLower() == "asc" 
                ? ordersQuery.OrderBy(o => o.OrderCode) 
                : ordersQuery.OrderByDescending(o => o.OrderCode),
            "status" => sortOrder.ToLower() == "asc" 
                ? ordersQuery.OrderBy(o => o.Status) 
                : ordersQuery.OrderByDescending(o => o.Status),
            "pickupaddress" => sortOrder.ToLower() == "asc" 
                ? ordersQuery.OrderBy(o => o.PickupLocationAddress) 
                : ordersQuery.OrderByDescending(o => o.PickupLocationAddress),
            "deliveryaddress" => sortOrder.ToLower() == "asc" 
                ? ordersQuery.OrderBy(o => o.DeliveryLocationAddress) 
                : ordersQuery.OrderByDescending(o => o.DeliveryLocationAddress),
            "createdat" => sortOrder.ToLower() == "asc" 
                ? ordersQuery.OrderBy(o => o.CreatedAt) 
                : ordersQuery.OrderByDescending(o => o.CreatedAt),
            _ => ordersQuery.OrderByDescending(o => o.CreatedAt)
        };

        var orders = ordersQuery.ToList();

        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.NextSortOrder = sortOrder.ToLower() == "asc" ? "desc" : "asc";

        return View(orders);
    }

    // ===== MANAGE CUSTOMERS =====
    public IActionResult ManageCustomers(string sortBy = "CreatedAt", string sortOrder = "desc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        var customersQuery = _context.Users
            .Where(u => !u.IsDeleted && u.Role == Role.Customer)
            .Include(u => u.Profile);

        IQueryable<User> orderedQuery;

        // Apply sorting
        switch (sortBy.ToLower())
        {
            case "id":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.Id) 
                    : customersQuery.OrderByDescending(u => u.Id);
                break;
            case "username":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.UserName) 
                    : customersQuery.OrderByDescending(u => u.UserName);
                break;
            case "email":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.Profile.Email) 
                    : customersQuery.OrderByDescending(u => u.Profile.Email);
                break;
            case "phone":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.Profile.Phone) 
                    : customersQuery.OrderByDescending(u => u.Profile.Phone);
                break;
            case "status":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.IsActive) 
                    : customersQuery.OrderByDescending(u => u.IsActive);
                break;
            case "createdat":
            default:
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? customersQuery.OrderBy(u => u.CreatedAt) 
                    : customersQuery.OrderByDescending(u => u.CreatedAt);
                break;
        }

        var customers = orderedQuery.ToList();

        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.NextSortOrder = sortOrder.ToLower() == "asc" ? "desc" : "asc";

        return View(customers);
    }

    // ===== MANAGE DRIVERS =====
    public IActionResult ManageDrivers(string sortBy = "Id", string sortOrder = "asc")
    {
        var auth = CheckAdmin();
        if (auth != null) return auth;

        // First get all drivers with their user IDs
        var driversQuery = _context.Drivers
            .Where(d => !d.IsDeleted);

        IQueryable<Driver> orderedQuery;

        // Apply sorting (only on Driver properties, not User properties)
        switch (sortBy.ToLower())
        {
            case "id":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? driversQuery.OrderBy(d => d.Id) 
                    : driversQuery.OrderByDescending(d => d.Id);
                break;
            case "licensenumber":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? driversQuery.OrderBy(d => d.LicenseNumber) 
                    : driversQuery.OrderByDescending(d => d.LicenseNumber);
                break;
            case "status":
                orderedQuery = sortOrder.ToLower() == "asc" 
                    ? driversQuery.OrderBy(d => d.Status) 
                    : driversQuery.OrderByDescending(d => d.Status);
                break;
            default:
                orderedQuery = driversQuery.OrderBy(d => d.Id);
                break;
        }

        var drivers = orderedQuery.ToList();

        // Get associated users
        var userIds = drivers.Select(d => d.UserId).ToList();
        var users = _context.Users
            .Where(u => userIds.Contains(u.Id))
            .Include(u => u.Profile)
            .ToDictionary(u => u.Id);

        ViewBag.Users = users;
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.NextSortOrder = sortOrder.ToLower() == "asc" ? "desc" : "asc";

        return View(drivers);
    }
}