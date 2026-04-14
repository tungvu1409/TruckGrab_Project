using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Models;
using TruckGrab.web.Data;

namespace TruckGrab.web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TestDb()
    {
        try
        {
            // Retrieve all users and their properties
            var users = _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Role,
                    u.IsActive,
                    u.IsDeleted,
                    u.CreatedAt,
                    u.UpdatedAt,
                    Profile = u.Profile != null ? new
                    {
                        u.Profile.FullName,
                        u.Profile.Email,
                        u.Profile.Phone,
                        u.Profile.Address
                    } : null,
                    Driver = u.Driver != null ? new
                    {
                        u.Driver.LicenseNumber,
                        u.Driver.LicenseClass,
                        u.Driver.ExperienceYears,
                        u.Driver.RatingAvg,
                        u.Driver.Status
                    } : null
                })
                .ToList();

            return Json(new { success = true, message = $"Database connection successful. Retrieved {users.Count} users.", users });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Database error: {ex.Message}" });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

