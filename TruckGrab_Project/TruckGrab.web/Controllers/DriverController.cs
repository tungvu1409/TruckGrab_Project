    using Microsoft.AspNetCore.Mvc;
    using TruckGrab.web.Models;
    using TruckGrab.web.Services;
    using TruckGrab.web.Services.Interface;
    using System.Security.Claims;

    namespace TruckGrab.web.Controllers;

    [Route("[controller]")]
    public class DriverController : Controller
    {
        private readonly IDriverService _driverService;
        private readonly IOrderService _orderService;
        private readonly ILogger<DriverController> _logger;
        private readonly DriverLocationStore _locationStore;

        public DriverController(
            IDriverService driverService,
            IOrderService orderService,
            ILogger<DriverController> logger,
            DriverLocationStore locationStore)
        {
            _driverService = driverService;
            _orderService = orderService;
            _logger = logger;
            _locationStore = locationStore;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var id))
                return id;
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            ViewBag.DriverName = User.Identity?.Name ?? "Driver";
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
            var orders = await GetDriverOrdersAsync(driver.Id);
            return View(orders);
        }

        [HttpGet("CreateProfile")]
        public async Task<IActionResult> CreateProfile()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            // Redirect to dashboard if profile already exists
            var existing = await _driverService.GetDriverByUserIdAsync(userId.Value);
            if (existing != null)
                return RedirectToAction("Index");

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
            var (order, logs) = await GetDriverOrderDetailAsync(id, driver.Id);
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

            var orders = await GetDriverOrdersAsync(driver.Id);
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

            // DEPRECATED: Drivers no longer choose orders. Admin assigns them.
            return Json(new List<object>());
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

            // DEPRECATED: Drivers no longer choose orders. Admin assigns them.
            return BadRequest("Order acceptance is now handled by Admin assignment only.");
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
                var success = await UpdateOrderStatusAsync(orderId, driver.Id, "InTransit", userId.Value, "Delivery started");
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
                return Json(new { success = false, message = "Not authenticated. Please log in again." });

            // Check if driver profile already exists
            var existingDriver = await _driverService.GetDriverByUserIdAsync(userId.Value);
            if (existingDriver != null)
                return Json(new { success = false, message = "A driver profile already exists for your account." });

            var driver = new Driver
            {
                UserId         = userId.Value,
                LicenseNumber  = request.LicenseNumber,
                LicenseClass   = request.LicenseClass,
                ExperienceYears = request.ExperienceYears,
                CurrentTruckId = request.CurrentTruckId,
                Status         = DriverStatus.Active,
                RatingAvg      = 0,
                IsDeleted      = false
            };

            try
            {
                await _driverService.CreateDriverAsync(driver);
                return Json(new { success = true, message = "Profile created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating driver profile for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while saving your profile. Please try again." });
            }
        }

        // Helper methods (these would typically be in a service layer)
        private async Task<IEnumerable<Order>> GetDriverOrdersAsync(int driverId)
        {
            return await _orderService.GetDriverOrdersAsync(driverId);
        }

        private async Task<(Order?, List<OrderStatusLog>)> GetDriverOrderDetailAsync(int orderId, int driverId)
        {
            return await _orderService.GetDriverOrderDetailAsync(orderId, driverId);
        }

        private async Task<IEnumerable<Order>> GetAvailableOrdersForDriverAsync()
        {
            return await _orderService.GetAvailableOrdersAsync();
        }

        private async Task<bool> AssignOrderToDriverAsync(int orderId, int driverId, int userId)
        {
            return await _orderService.AssignOrderToDriverAsync(orderId, driverId, userId);
        }

        private async Task<bool> UpdateOrderStatusAsync(int orderId, int driverId, string newStatus, int userId, string note)
        {
            // First verify the order belongs to this driver
            var (order, _) = await _orderService.GetDriverOrderDetailAsync(orderId, driverId);
            if (order == null)
                return false;

            return await _orderService.UpdateOrderStatusAsync(orderId, newStatus, userId, note);
        }

        // ============================
        // GPS LOCATION (In-Memory)
        // ============================

        [HttpPost("UpdateLocation")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
            if (driver == null)
                return NotFound(new { message = "Driver profile not found" });

            _logger.LogInformation("[GPS] Updating location for driver {DriverId} ({UserName}): {Lat}, {Lng}", driver.Id, User.Identity?.Name, request.Lat, request.Lng);

            _locationStore.UpdateLocation(
                driver.Id,
                User.Identity?.Name ?? "Driver",
                driver.Status.ToString(),
                request.Lat,
                request.Lng);

            return Json(new { success = true, updatedAt = DateTime.UtcNow });
        }

        [HttpGet("GetMyLocation")]
        public async Task<IActionResult> GetMyLocation()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
            if (driver == null)
                return NotFound();

            var loc = _locationStore.GetLocation(driver.Id);
            if (loc == null)
                return Json(new { lat = (double?)null, lng = (double?)null });

            return Json(new { lat = loc.Lat, lng = loc.Lng, updatedAt = loc.UpdatedAt });
        }

        [HttpPost("GoOffline")]
        public async Task<IActionResult> GoOffline()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var driver = await _driverService.GetDriverByUserIdAsync(userId.Value);
            if (driver != null)
            {
                _locationStore.RemoveLocation(driver.Id);
            }

            return Json(new { success = true });
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

    public class UpdateLocationRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
