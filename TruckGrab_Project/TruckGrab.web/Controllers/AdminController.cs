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
}