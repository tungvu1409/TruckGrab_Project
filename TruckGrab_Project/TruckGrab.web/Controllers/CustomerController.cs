using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Services.Interface;
using TruckGrab.web.Models;
using System.Security.Cryptography;
using System.Text.Json;
using System.Security.Claims;

namespace TruckGrab.web.Controllers;


[ApiController]
[Route("[controller]")]
public class CustomerController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;
    private readonly IGeolocationService _geolocationService;

    public CustomerController(IOrderService orderService, IConfiguration configuration, IGeolocationService geolocationService)
    {
        _orderService = orderService;
        _configuration = configuration;
        _geolocationService = geolocationService;
    }

    // ========================
    // AUTH CHECK
    // ========================
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

    // ========================
    // DASHBOARD
    // ========================
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View();
    }

    // ========================
    // LIST
    // ========================
    [HttpGet("Order/History")]
    public async Task<IActionResult> Orders()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orders = await _orderService.GetOrdersAsync(GetUserId());
        return View("Order/Orders", orders);
    }

    // ========================
    // CREATE
    // ========================

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

        if (!ModelState.IsValid)
            return View("Order/CreateOrder", model);
            
        // Store partial order in TempData for next step
        TempData["Order"] = System.Text.Json.JsonSerializer.Serialize(model);

        return RedirectToAction("SelectAddress");
    }

    // ========================
    // SELECT ADDRESS
    // ========================

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

        // Merge the addresses
        partialOrder.PickupAddress = model.PickupAddress;
        partialOrder.DeliveryAddress = model.DeliveryAddress;

        TempData["Order"] = System.Text.Json.JsonSerializer.Serialize(partialOrder);

        return RedirectToAction("ConfirmOrder");
    }

    // ========================
    // CONFIRM ORDER
    // ========================

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

        // Calculate Distance
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
                // Fallback to haversine if API fails
                model.DistanceKm = _geolocationService.CalculateHaversineDistance(pickupGeo.Coordinates, deliveryGeo.Coordinates);
            }
        }
        else
        {
             ModelState.AddModelError(string.Empty, "Could not geocode one or more addresses. Distance and price might not be accurate.");
        }

        model.TotalPrice = _orderService.CalculatePrice(model.DistanceKm, model.CargoType, model.Weight);
        
        // Save the updated model back to TempData so we have Distance and Price
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

        // Now create the order
        await _orderService.CreateOrderAsync(partialOrder, GetUserId());

        return RedirectToAction("OrderSuccess", new { id = partialOrder.Id });
    }

    // ========================
    // ORDER SUCCESS
    // ========================
    [HttpGet("Order/Success/{id}")]
    public IActionResult OrderSuccess(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        ViewBag.OrderId = id;
        return View("Order/OrderSuccess");
    }

    // ========================
    // DETAILS
    // ========================
    [HttpGet("/Orders/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, logs) = await _orderService.GetOrderDetailAsync(id, GetUserId());

        if (order == null) return NotFound();

        ViewBag.Logs = logs;
        return View("Order/Details", order);
    }

    // ========================
    // EDIT
    // ========================
    [HttpGet("/Orders/Edit/{id}")]
    public async Task<IActionResult> EditOrder(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, _) = await _orderService.GetOrderDetailAsync(id, GetUserId());

        if (order == null) return NotFound();

        return View("Order/EditOrder", order);
    }

    [HttpPost("/Orders/Edit/{id}")]
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

    // ========================
    // CANCEL
    // ========================
    [HttpPost("/Orders/Cancel")]
    public async Task<IActionResult> CancelOrder(int id, string reason)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!await _orderService.CancelOrderAsync(id, GetUserId(), reason))
            return BadRequest();

        return RedirectToAction("Orders");
    }
}