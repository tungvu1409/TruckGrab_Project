using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Data;
using TruckGrab.web.Models;

namespace TruckGrab.web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== LOGIN =====

    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string userName, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }

        HttpContext.Session.SetString("UserName", user.UserName);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        return user.Role switch
        {
            
           Role.Admin => RedirectToAction("Index", "Admin"),
            Role.Driver => RedirectToAction("Index", "Driver"),
            Role.Customer => RedirectToAction("Index", "Customer"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string userName, string email, string password, string confirmPassword, string role)
    {
        if (password != confirmPassword)
        {
            ViewBag.Error = "Passwords do not match";
            return View();
        }

        var exists = _context.Users.Any(u => u.UserName == userName);
        if (exists)
        {
            ViewBag.Error = "Username already exists";
            return View();
        }

        if (!Enum.TryParse<Role>(role, out var parsedRole))
        {
            parsedRole = Role.Customer;
        }

        var user = new User
        {
            UserName = userName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = parsedRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return RedirectToAction("Login");
    }


    [HttpGet("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    
}