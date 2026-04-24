using TruckGrab.web.Models;

namespace TruckGrab.web.Services.Interface;

public interface IDriverService
{
    Task<Driver?> GetDriverByIdAsync(int id);
    Task<Driver?> GetDriverByUserIdAsync(int userId);
    Task<List<Driver>> GetAllActiveDriversAsync();
    Task<List<Driver>> GetAvailableDriversAsync();
    Task<Driver> CreateDriverAsync(Driver driver);
    Task<Driver> UpdateDriverAsync(Driver driver);
    Task<bool> DeleteDriverAsync(int id);
    Task<bool> UpdateDriverStatusAsync(int driverId, DriverStatus status);
    Task<decimal> GetAverageRatingAsync(int driverId);
    Task<List<Driver>> GetDriversByStatusAsync(DriverStatus status);
}
