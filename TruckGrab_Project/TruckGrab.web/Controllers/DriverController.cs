using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverController : Controller
{
    private readonly IDriverService _driverService;
    private readonly ILogger<DriverController> _logger;

   
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
