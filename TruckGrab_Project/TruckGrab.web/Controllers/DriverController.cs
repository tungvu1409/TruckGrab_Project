using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverController : Controller
{
    private readonly IDriverService _driverService;
    private readonly IOrderService _orderService;
    private readonly ILogger<DriverController> _logger;

    public DriverController(IDriverService driverService, IOrderService orderService, ILogger<DriverController> logger)
    {
        _driverService = driverService;
        _orderService = orderService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
    }

    // MVC Actions
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        ViewBag.DriverName = HttpContext.Session.GetString("UserName") ?? "Driver";
        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        ViewBag.HasDriverProfile = driver != null;

        return View();
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        return View(driver);
    }

    [HttpGet("MyOrders")]
    public async Task<IActionResult> MyOrders()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return RedirectToAction("CreateProfile");

        // Get orders assigned to this driver
        var orders = GetDriverOrders(driver.Id);
        return View(orders);
    }

    [HttpGet("CreateProfile")]
    public IActionResult CreateProfile()
    {
        return View();
    }

    [HttpGet("OrderDetails/{id}")]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return RedirectToAction("CreateProfile");

        // Get order details for driver
        var (order, logs) = GetDriverOrderDetail(id, driver.Id);
        if (order == null)
            return NotFound();

        ViewBag.OrderLogs = logs;
        return View(order);
    }

    // API Actions
    [HttpGet("GetMyProfile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound();

        return Json(new
        {
            id = driver.Id,
            licenseNumber = driver.LicenseNumber,
            licenseClass = driver.LicenseClass,
            experienceYears = driver.ExperienceYears,
            ratingAvg = driver.RatingAvg,
            status = driver.Status.ToString(),
            currentTruckId = driver.CurrentTruckId
        });
    }

    [HttpGet("GetMyOrders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound();

        var orders = GetDriverOrders(driver.Id);
        var recentOrders = orders.Take(5).Select(o => new
        {
            id = o.Id,
            orderCode = o.OrderCode,
            cargoType = o.CargoType,
            pickupLocation = o.PickupAddress,
            deliveryLocation = o.DeliveryAddress,
            status = o.Status
        });

        return Json(new
        {
            totalOrders = orders.Count(),
            completedOrders = orders.Count(o => o.Status == "Delivered"),
            recentOrders = recentOrders
        });
    }

    [HttpGet("GetAvailableOrders")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound();

        // Get orders that are pending and not assigned to any driver
        var availableOrders = GetAvailableOrdersForDriver();
        var result = availableOrders.Select(o => new
        {
            id = o.Id,
            orderCode = o.OrderCode,
            cargoType = o.CargoType,
            weight = o.Weight,
            distanceKm = o.DistanceKm,
            totalPrice = o.TotalPrice,
            scheduledPickupTime = o.ScheduledPickupTime
        });

        return Json(result);
    }

    [HttpPost("AcceptOrder/{orderId}")]
    public async Task<IActionResult> AcceptOrder(int orderId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound("Driver profile not found");

        if (driver.Status != DriverStatus.Available)
            return BadRequest("Driver is not available to accept orders");

        try
        {
            var success = await AssignOrderToDriver(orderId, driver.Id, userId.Value);
            if (success)
            {
                // Update driver status to OnDelivery
                await _driverService.UpdateDriverStatusAsync(driver.Id, DriverStatus.OnDelivery);
                return Json(new { success = true, message = "Order accepted successfully" });
            }
            else
            {
                return BadRequest("Failed to accept order");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting order {OrderId} for driver {DriverId}", orderId, driver.Id);
            return BadRequest("Error accepting order");
        }
    }

    [HttpPost("UpdateProfile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound();

        driver.LicenseNumber = request.LicenseNumber;
        driver.LicenseClass = request.LicenseClass;
        driver.ExperienceYears = request.ExperienceYears;
        driver.CurrentTruckId = request.CurrentTruckId;

        if (Enum.TryParse<DriverStatus>(request.Status, out var status))
        {
            driver.Status = status;
        }

        try
        {
            await _driverService.UpdateDriverAsync(driver);
            return Json(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver profile for user {UserId}", userId);
            return BadRequest("Error updating profile");
        }
    }

    [HttpPost("StartDelivery/{orderId}")]
    public async Task<IActionResult> StartDelivery(int orderId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (driver == null)
            return NotFound();

        try
        {
            var success = await UpdateOrderStatus(orderId, driver.Id, "InTransit", userId.Value, "Delivery started");
            if (success)
            {
                return Json(new { success = true, message = "Delivery started successfully" });
            }
            else
            {
                return BadRequest("Failed to start delivery");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting delivery for order {OrderId}", orderId);
            return BadRequest("Error starting delivery");
        }
    }

    [HttpPost("CreateProfile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Check if driver profile already exists
        var existingDriver = await _driverService.GetDriverByUserIdAsync(userId.Value);
        if (existingDriver != null)
            return BadRequest("Driver profile already exists");

        var driver = new Driver
        {
            UserId = userId.Value,
            LicenseNumber = request.LicenseNumber,
            LicenseClass = request.LicenseClass,
            ExperienceYears = request.ExperienceYears,
            CurrentTruckId = request.CurrentTruckId,
            Status = DriverStatus.Available, // Default to available, admin can change later
            RatingAvg = 0,
            IsDeleted = false
        };

        try
        {
            await _driverService.CreateDriverAsync(driver);
            return Json(new { success = true, message = "Profile created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver profile for user {UserId}", userId);
            return BadRequest("Error creating profile");
        }
    }

    // Helper methods (these would typically be in a service layer)
    private IEnumerable<Order> GetDriverOrders(int driverId)
    {
        return _orderService.GetDriverOrders(driverId);
    }

    private (Order?, List<OrderStatusLog>) GetDriverOrderDetail(int orderId, int driverId)
    {
        return _orderService.GetDriverOrderDetail(orderId, driverId);
    }

    private IEnumerable<Order> GetAvailableOrdersForDriver()
    {
        return _orderService.GetAvailableOrders();
    }

    private async Task<bool> AssignOrderToDriver(int orderId, int driverId, int userId)
    {
        return _orderService.AssignOrderToDriver(orderId, driverId, userId);
    }

    private async Task<bool> UpdateOrderStatus(int orderId, int driverId, string newStatus, int userId, string note)
    {
        // First verify the order belongs to this driver
        var (order, _) = _orderService.GetDriverOrderDetail(orderId, driverId);
        if (order == null)
            return false;

        return _orderService.UpdateOrderStatus(orderId, newStatus, userId, note);
    }
}

public class CreateProfileRequest
{
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public int? CurrentTruckId { get; set; }
}

public class UpdateProfileRequest
{
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseClass { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CurrentTruckId { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
