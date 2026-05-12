using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Data;
using TruckGrab.web.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace TruckGrab.web.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public AccountController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env)
    {
        _context = context;
        _configuration = configuration;
        _env = env;
    }

    // ===== LOGIN =====

    [HttpGet("Login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string userName, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.UserName == userName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }

        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        Response.Cookies.Append("jwt_token", jwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // Only require HTTPS in production
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return user.Role switch
        {
           Role.Admin => RedirectToAction("Index", "Admin"),
            Role.Driver => RedirectToAction("Index", "Driver"),
            Role.Customer => RedirectToAction("Index", "Customer"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("Register")]
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


    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt_token");
        return RedirectToAction("Index", "Home");
    }

    
}