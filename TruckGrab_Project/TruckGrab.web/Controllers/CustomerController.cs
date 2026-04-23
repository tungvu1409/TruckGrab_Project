using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Services.Interface;
using TruckGrab.web.Models;
using System.Security.Cryptography;

namespace TruckGrab.web.Controllers;

[Route("customer")]
public class CustomerController : Controller
{
    private readonly IOrderService _orderService;

    public CustomerController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // ========================
    // AUTH CHECK
    // ========================
    private IActionResult? CheckCustomer()
    {
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(role))
            return RedirectToAction("Login", "Account");

        if (role != "Customer")
            return Forbid();

        return null;
    }

    private int GetUserId() =>
        HttpContext.Session.GetInt32("UserId") ?? 0;

    // ========================
    // DASHBOARD
    // ========================
    [HttpGet("")]
    [HttpGet("index")]
    public IActionResult Index()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View();
    }

    // ========================
    // LIST
    // ========================
    [HttpGet("orders")]
    public IActionResult Orders()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var orders = _orderService.GetOrders(GetUserId());
        return View("Order/Orders", orders);
    }

    // ========================
    // CREATE
    // ========================

    [HttpGet("create")]
    public IActionResult CreateOrder()
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        return View("Order/CreateOrder", new Order());
    }

    [HttpPost("create")]
    public IActionResult CreateOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View("Order/CreateOrder", model);
            
        _orderService.CreateOrder(model, GetUserId());

        return RedirectToAction("Orders");
    }

    // ========================
    // DETAILS
    // ========================
    [HttpGet("details/{id}")]
    [HttpGet("/Orders/Details/{id}")]
    public IActionResult Details(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, logs) = _orderService.GetOrderDetail(id, GetUserId());

        if (order == null) return NotFound();

        ViewBag.Logs = logs;
        return View("Order/Details", order);
    }

    // ========================
    // EDIT
    // ========================
    [HttpGet("edit/{id}")]
    [HttpGet("/Orders/Edit/{id}")]
    public IActionResult EditOrder(int id)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        var (order, _) = _orderService.GetOrderDetail(id, GetUserId());

        if (order == null) return NotFound();

        return View("Order/EditOrder", order);
    }

    [HttpPost("edit/{id}")]
    public IActionResult EditOrder(Order model)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!ModelState.IsValid)
            return View("Order/EditOrder", model);

        if (!_orderService.UpdateOrder(model, GetUserId()))
            return BadRequest();

        return RedirectToAction("Details", new { id = model.Id });
    }

    // ========================
    // CANCEL
    // ========================
    [HttpPost("cancel")]
    public IActionResult CancelOrder(int id, string reason)
    {
        var auth = CheckCustomer();
        if (auth != null) return auth;

        if (!_orderService.CancelOrder(id, GetUserId(), reason))
            return BadRequest();

        return RedirectToAction("Orders");
    }
}