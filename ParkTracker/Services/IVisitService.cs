using ParkTracker.Data.Models;

namespace ParkTracker.Services;

public interface IVisitService
{
    Task AddVisitAsync(int parkId, string userId, DateTime? visitDate);
    Task<List<Visit>> GetVisitsForUserAsync(string userId);
}
