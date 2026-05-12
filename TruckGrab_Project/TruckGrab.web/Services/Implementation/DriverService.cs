using Microsoft.EntityFrameworkCore;
using TruckGrab.web.Data;
using TruckGrab.web.Models;
using TruckGrab.web.Services.Interface;

namespace TruckGrab.web.Services.Implementation;

public class DriverService : IDriverService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DriverService> _logger;

    public DriverService(ApplicationDbContext context, ILogger<DriverService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Driver?> GetDriverByIdAsync(int id)
    {
        try
        {
            return await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver by ID: {DriverId}", id);
            return null;
        }
    }

    public async Task<Driver?> GetDriverByUserIdAsync(int userId)
    {
        try
        {
            return await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver by User ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<List<Driver>> GetAllActiveDriversAsync()
    {
        try
        {
            return await _context.Drivers
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active drivers");
            return new List<Driver>();
        }
    }

    public async Task<List<Driver>> GetAvailableDriversAsync()
    {
        try
        {
            return await _context.Drivers
                .AsNoTracking()
                .Where(d => !d.IsDeleted && d.Status == DriverStatus.Active)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available drivers");
            return new List<Driver>();
        }
    }

    public async Task<Driver> CreateDriverAsync(Driver driver)
    {
        try
        {
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Driver created successfully: {DriverId}", driver.Id);
            return driver;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating driver");
            throw;
        }
    }

    public async Task<Driver> UpdateDriverAsync(Driver driver)
    {
        try
        {
            _context.Drivers.Update(driver);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Driver updated successfully: {DriverId}", driver.Id);
            return driver;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver: {DriverId}", driver.Id);
            throw;
        }
    }

    public async Task<bool> DeleteDriverAsync(int id)
    {
        try
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                _logger.LogWarning("Driver not found: {DriverId}", id);
                return false;
            }

            driver.IsDeleted = true;
            _context.Drivers.Update(driver);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Driver deleted (soft delete): {DriverId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting driver: {DriverId}", id);
            return false;
        }
    }

    public async Task<bool> UpdateDriverStatusAsync(int driverId, DriverStatus status)
    {
        try
        {
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null)
            {
                _logger.LogWarning("Driver not found: {DriverId}", driverId);
                return false;
            }

            driver.Status = status;
            _context.Drivers.Update(driver);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Driver status updated: {DriverId} -> {Status}", driverId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver status: {DriverId}", driverId);
            return false;
        }
    }

    public async Task<decimal> GetAverageRatingAsync(int driverId)
    {
        try
        {
            var driver = await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == driverId);

            return driver?.RatingAvg ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver rating: {DriverId}", driverId);
            return 0;
        }
    }

    public async Task<List<Driver>> GetDriversByStatusAsync(DriverStatus status)
    {
        try
        {
            return await _context.Drivers
                .AsNoTracking()
                .Where(d => !d.IsDeleted && d.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drivers by status: {Status}", status);
            return new List<Driver>();
        }
    }
}
