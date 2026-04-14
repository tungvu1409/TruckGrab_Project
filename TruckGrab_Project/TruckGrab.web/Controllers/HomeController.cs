using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TruckGrab.web.Models;
using TruckGrab.web.Data;

namespace TruckGrab.web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult TestDb()
    {
        try
        {
            var result = new
            {
                Users = new
                {
                    Count = _context.Users.Count(),
                    Sample = _context.Users
                        .Select(u => new
                        {
                            u.Id,
                            u.UserName,
                            u.Role,
                            u.IsActive,
                            u.IsDeleted,
                            u.CreatedAt,
                            u.UpdatedAt
                        })
                        .Take(5)
                        .ToList()
                },
                UserProfiles = new
                {
                    Count = _context.UserProfiles.Count(),
                    Sample = _context.UserProfiles
                        .Select(p => new
                        {
                            p.UserId,
                            p.FullName,
                            p.Email,
                            p.Phone,
                            p.AvatarUrl,
                            p.Address
                        })
                        .Take(5)
                        .ToList()
                },
                Drivers = new
                {
                    Count = _context.Drivers.Count(),
                    Sample = _context.Drivers
                        .Select(d => new
                        {
                            d.Id,
                            d.UserId,
                            d.LicenseNumber,
                            d.LicenseClass,
                            d.ExperienceYears,
                            d.RatingAvg,
                            d.Status,
                            d.CurrentTruckId,
                            d.IsDeleted
                        })
                        .Take(5)
                        .ToList()
                },
                DriverRatings = new
                {
                    Count = _context.DriverRatings.Count(),
                    Sample = _context.DriverRatings
                        .Select(r => new
                        {
                            r.Id,
                            r.OrderId,
                            r.CustomerId,
                            r.DriverId,
                            r.Score,
                            r.Comment,
                            r.CreatedAt
                        })
                        .Take(5)
                        .ToList()
                },
                FuelLogs = new
                {
                    Count = _context.FuelLogs.Count(),
                    Sample = _context.FuelLogs
                        .Select(f => new
                        {
                            f.Id,
                            f.TruckId,
                            f.DriverId,
                            f.RefuelDate,
                            f.Liters,
                            f.CostAmount
                        })
                        .Take(5)
                        .ToList()
                },
                Locations = new
                {
                    Count = _context.Locations.Count(),
                    Sample = _context.Locations
                        .Select(l => new
                        {
                            l.Id,
                            l.Name,
                            l.Address,
                            l.Lat,
                            l.Lng,
                            l.Type
                        })
                        .Take(5)
                        .ToList()
                },
                Orders = new
                {
                    Count = _context.Orders.Count(),
                    Sample = _context.Orders
                        .Select(o => new
                        {
                            o.Id,
                            o.OrderCode,
                            o.CustomerId,
                            o.PickupLocId,
                            o.DeliveryLocId,
                            o.CargoType,
                            o.Weight,
                            o.DistanceKm,
                            o.TotalPrice,
                            o.Status,
                            o.ScheduledPickupTime,
                            o.ActualPickupTime,
                            o.ActualDeliveryTime,
                            o.CancelledBy,
                            o.CancelledReason,
                            o.IsDeleted,
                            o.CreatedAt,
                            o.UpdatedAt
                        })
                        .Take(5)
                        .ToList()
                },
                OrderStatusLogs = new
                {
                    Count = _context.OrderStatusLogs.Count(),
                    Sample = _context.OrderStatusLogs
                        .Select(s => new
                        {
                            s.Id,
                            s.OrderId,
                            s.OldStatus,
                            s.NewStatus,
                            s.ChangedByUserId,
                            s.Timestamp,
                            s.Note
                        })
                        .Take(5)
                        .ToList()
                },
                Payments = new
                {
                    Count = _context.Payments.Count(),
                    Sample = _context.Payments
                        .Select(p => new
                        {
                            p.Id,
                            p.OrderId,
                            p.Amount,
                            p.PaymentMethod,
                            p.Status,
                            p.TransactionCode,
                            p.PaidAt,
                            p.CreatedAt
                        })
                        .Take(5)
                        .ToList()
                },
                Trips = new
                {
                    Count = _context.Trips.Count(),
                    Sample = _context.Trips
                        .Select(t => new
                        {
                            t.Id,
                            t.OrderId,
                            t.TruckId,
                            t.DriverId,
                            t.StartTime,
                            t.EndTime,
                            t.ActualRouteUrl
                        })
                        .Take(5)
                        .ToList()
                },
                Trucks = new
                {
                    Count = _context.Trucks.Count(),
                    Sample = _context.Trucks
                        .Select(t => new
                        {
                            t.Id,
                            t.LicensePlate,
                            t.TruckTypeId,
                            t.Brand,
                            t.Model,
                            t.FuelType,
                            t.Status,
                            t.CurrentLat,
                            t.CurrentLng,
                            t.IsDeleted
                        })
                        .Take(5)
                        .ToList()
                },
                TruckDriverAssignments = new
                {
                    Count = _context.TruckDriverAssignments.Count(),
                    Sample = _context.TruckDriverAssignments
                        .Select(a => new
                        {
                            a.Id,
                            a.TruckId,
                            a.DriverId,
                            a.AssignedAt,
                            a.IsPrimary
                        })
                        .Take(5)
                        .ToList()
                }
            };

            return Json(new { success = true, message = "Database test completed successfully.", result });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Database error: {ex.Message}", detail = ex.ToString() });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

