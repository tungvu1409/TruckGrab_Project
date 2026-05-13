using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Models;
using TruckGrab.web.Data;

namespace TruckGrab.web.Controllers;    

[Route("")]
[Route("Home")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet("/")]
    public IActionResult Root()
    {
        return Redirect("/Home/Index");
    }

    [HttpGet("Index")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");
            if (User.IsInRole("Driver"))
                return RedirectToAction("Index", "Driver");
            if (User.IsInRole("Customer"))
                return RedirectToAction("Index", "Customer");
        }
        
        return View();
    }

    [HttpGet("Privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

