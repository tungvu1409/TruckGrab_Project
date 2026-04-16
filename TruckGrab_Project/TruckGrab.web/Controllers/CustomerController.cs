using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Services.Interface;
using TruckGrab.web.Models;

namespace TruckGrab.web.Controllers;

public class CustomerController : Controller
{
    private readonly IOrderService _orderService;

    public CustomerController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private IActionResult CheckCustomer()
    {
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
            return RedirectToAction("Login", "Account");

        if (role != "Customer")
            return Forbid();

        return null!;
    }

    public IActionResult Index()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View();
    }

    private int GetUserId() =>
        HttpContext.Session.GetInt32("UserId")!.Value;

    // ========================
    // LIST
    // ========================
    public IActionResult Orders()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orders = _orderService.GetOrders(GetUserId());
        return View(orders);
    }

    // ========================
    // CREATE
    // ========================
    public IActionResult CreateOrder()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View();
    }

    [HttpPost]
    public IActionResult CreateOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View(model);

        _orderService.CreateOrder(model, GetUserId());

        return RedirectToAction("Orders");
    }

    // ========================
    // DETAILS
    // ========================
    public IActionResult OrderDetails(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, logs) = _orderService.GetOrderDetail(id, GetUserId());

        if (order == null) return NotFound();

        ViewBag.Logs = logs;
        return View(order);
    }

    // ========================
    // EDIT
    // ========================
    public IActionResult EditOrder(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, _) = _orderService.GetOrderDetail(id, GetUserId());

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    public IActionResult EditOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View(model);

        if (!_orderService.UpdateOrder(model, GetUserId()))
            return BadRequest();

        return RedirectToAction("Orders");
    }

    // ========================
    // CANCEL
    // ========================
    [HttpPost]
    public IActionResult CancelOrder(int id, string reason)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!_orderService.CancelOrder(id, GetUserId(), reason))
            return BadRequest();

        return RedirectToAction("Orders");
    }
}