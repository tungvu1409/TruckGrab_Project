using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Services.Interface;
using TruckGrab.web.Models;
using System.Security.Cryptography;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TruckGrab.web.Controllers;

[Route("[controller]")]
public class CustomerController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;
    private readonly IGeolocationService _geolocationService;
    private readonly TruckGrab.web.Data.ApplicationDbContext _context;

    public CustomerController(IOrderService orderService, IConfiguration configuration, IGeolocationService geolocationService, TruckGrab.web.Data.ApplicationDbContext context)
    {
        _orderService = orderService;
        _configuration = configuration;
        _geolocationService = geolocationService;
        _context = context;
    }

    private IActionResult? CheckCustomer()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToAction("Login", "Account");

        if (!User.IsInRole("Customer"))
            return Forbid();

        return null;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
    [HttpGet("GetOrdersJson")]
    public async Task<IActionResult> GetOrdersJson()
    {
        var auth = CheckCustomer();
        if (auth != null) return Unauthorized();

        var orders = await _orderService.GetOrdersAsync(GetUserId());
        var activeCount = orders.Count(o => o.Status != "delivered" && o.Status != "cancelled");
        var doneCount = orders.Count(o => o.Status == "delivered");

        return Json(new { activeCount, doneCount });
    }

    [HttpGet("GetActiveOrdersJson")]
    public async Task<IActionResult> GetActiveOrdersJson()
    {
        var auth = CheckCustomer();
        if (auth != null) return Unauthorized();

        var orders = await _orderService.GetOrdersAsync(GetUserId());
        var activeOrders = orders
            .Where(o => o.Status != "delivered" && o.Status != "cancelled")
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new {
                o.Id,
                o.OrderCode,
                o.Status,
                o.PickupAddress,
                o.DeliveryAddress,
                o.TotalPrice,
                CreatedAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });

        return Json(activeOrders);
    }


    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View();
    }

    [HttpGet("Order/History")]
    public async Task<IActionResult> Orders()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orders = await _orderService.GetOrdersAsync(GetUserId());
        return View("Order/Orders", orders);
    }

    [HttpGet("Order/New")]
    public IActionResult CreateOrder()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View("Order/CreateOrder", new Order());
    }

    [HttpPost("Order/New")]
    public async Task<IActionResult> CreateOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        ModelState.Clear();
        if (model.Weight <= 0) ModelState.AddModelError("Weight", "Vui lòng nhập khối lượng hợp lệ");
        if (string.IsNullOrEmpty(model.CargoType)) ModelState.AddModelError("CargoType", "Vui lòng chọn loại hàng");

        if (!ModelState.IsValid)
            return View("Order/CreateOrder", model);
            
        TempData["Order"] = System.Text.Json.JsonSerializer.Serialize(model);

        return RedirectToAction("SelectAddress");
    }

    [HttpGet("Order/New/Address")]
    public IActionResult SelectAddress()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orderJson = TempData["Order"] as string;
        if (string.IsNullOrEmpty(orderJson))
            return RedirectToAction("CreateOrder");

        var model = System.Text.Json.JsonSerializer.Deserialize<Order>(orderJson);
        TempData.Keep("Order");

        return View("Order/SelectAddress", model);
    }

    [HttpPost("Order/New/Address")]
    public async Task<IActionResult> SelectAddress(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        ModelState.Clear();
        if (string.IsNullOrEmpty(model.PickupAddress)) ModelState.AddModelError("PickupAddress", "Vui lòng chọn điểm nhận");
        if (string.IsNullOrEmpty(model.DeliveryAddress)) ModelState.AddModelError("DeliveryAddress", "Vui lòng chọn điểm giao");

        if (!ModelState.IsValid)
        {
            return View("Order/SelectAddress", model);
        }

        var orderJson = TempData["Order"] as string;
        if (string.IsNullOrEmpty(orderJson))
            return RedirectToAction("CreateOrder");

        var partialOrder = System.Text.Json.JsonSerializer.Deserialize<Order>(orderJson);
        if (partialOrder == null)
            return RedirectToAction("CreateOrder");

        partialOrder.PickupAddress = model.PickupAddress;
        partialOrder.DeliveryAddress = model.DeliveryAddress;

        TempData["Order"] = System.Text.Json.JsonSerializer.Serialize(partialOrder);

        return RedirectToAction("ConfirmOrder");
    }

    [HttpGet("Order/New/Confirm")]
    public async Task<IActionResult> ConfirmOrder()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orderJson = TempData["Order"] as string;
        if (string.IsNullOrEmpty(orderJson))
            return RedirectToAction("CreateOrder");

        var model = System.Text.Json.JsonSerializer.Deserialize<Order>(orderJson);
        TempData.Keep("Order");

        if (model == null)
            return RedirectToAction("CreateOrder");

        var pickupGeo = await _geolocationService.GeocodeAddressAsync(model.PickupAddress);
        var deliveryGeo = await _geolocationService.GeocodeAddressAsync(model.DeliveryAddress);

        if (pickupGeo.Success && pickupGeo.Coordinates != null && 
            deliveryGeo.Success && deliveryGeo.Coordinates != null)
        {
            var distanceResult = await _geolocationService.GetDistanceAsync(
                pickupGeo.Coordinates,
                deliveryGeo.Coordinates
            );

            if (distanceResult.Success)
            {
                model.DistanceKm = distanceResult.DistanceMeters / 1000m;
            }
            else
            {
                model.DistanceKm = _geolocationService.CalculateHaversineDistance(pickupGeo.Coordinates, deliveryGeo.Coordinates);
            }
        }
        else
        {
             ModelState.AddModelError(string.Empty, "Could not geocode one or more addresses. Distance and price might not be accurate.");
        }

        model.TotalPrice = _orderService.CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);
        
        TempData["Order"] = System.Text.Json.JsonSerializer.Serialize(model);

        return View("Order/ConfirmOrder", model);
    }

    [HttpPost("Order/New/Confirm")]
    public async Task<IActionResult> ConfirmOrderPost()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orderJson = TempData["Order"] as string;
        if (string.IsNullOrEmpty(orderJson))
            return RedirectToAction("CreateOrder");

        var partialOrder = System.Text.Json.JsonSerializer.Deserialize<Order>(orderJson);
        if (partialOrder == null)
            return RedirectToAction("CreateOrder");

        await _orderService.CreateOrderAsync(partialOrder, GetUserId());

        return RedirectToAction("OrderSuccess", new { id = partialOrder.Id });
    }

    [HttpGet("Order/Success/{id}")]
    public async Task<IActionResult> OrderSuccess(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, _) = await _orderService.GetOrderDetailAsync(id, GetUserId());
        if (order == null) return NotFound();

        ViewBag.OrderId = order.Id;
        ViewBag.OrderCode = order.OrderCode;
        return View("Order/OrderSuccess");
    }

    [HttpGet("/Order/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, logs) = await _orderService.GetOrderDetailAsync(id, GetUserId());

        if (order == null) return NotFound();

        ViewBag.Logs = logs;
        return View("Order/Details", order);
    }

    [HttpGet("/Order/Edit/{id}")]
    public async Task<IActionResult> EditOrder(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, _) = await _orderService.GetOrderDetailAsync(id, GetUserId());

        if (order == null) return NotFound();

        return View("Order/EditOrder", order);
    }

    [HttpPost("/Order/Edit/{id}")]
    public async Task<IActionResult> EditOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View("Order/EditOrder", model);

        if (!await _orderService.UpdateOrderAsync(model, GetUserId()))
            return BadRequest();

        return RedirectToAction("Details", new { id = model.Id });
    }

    [HttpPost("/Order/Cancel")]
    public async Task<IActionResult> CancelOrder(int id, string reason)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!await _orderService.CancelOrderAsync(id, GetUserId(), reason))
            return BadRequest();

        return RedirectToAction("Orders");
    }

    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return RedirectToAction("Profile", "Account");
    }
}