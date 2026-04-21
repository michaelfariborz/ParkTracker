using ParkTracker.Data.Models;

namespace ParkTracker.Services;

public interface IParkService
{
    Task<List<Park>> GetAllParksWithUserVisitsAsync(string userId);
    Task<List<Park>> GetAllParksAsync();
    Task AddParkAsync(string name, string state, double latitude, double longitude, string? description = null);
}
