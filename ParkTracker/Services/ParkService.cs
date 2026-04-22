using Microsoft.EntityFrameworkCore;
using ParkTracker.Data;
using ParkTracker.Data.Models;

namespace ParkTracker.Services;

public class ParkService(ApplicationDbContext db) : IParkService
{
    public async Task<List<Park>> GetAllParksWithUserVisitsAsync(string userId) =>
        await db.Parks
            .Include(p => p.Visits.Where(v => v.UserId == userId))
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<List<Park>> GetAllParksAsync() =>
        await db.Parks
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task AddParkAsync(string name, string state, double latitude, double longitude, string? description = null)
    {
        db.Parks.Add(new Park
        {
            Name = name,
            State = state,
            Latitude = latitude,
            Longitude = longitude,
            Description = description
        });
        await db.SaveChangesAsync();
    }
}
