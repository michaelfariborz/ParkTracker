using Microsoft.EntityFrameworkCore;
using ParkTracker.Data;
using ParkTracker.Data.Models;

namespace ParkTracker.Services;

public class VisitService(ApplicationDbContext db)
{
    public async Task AddVisitAsync(int parkId, string userId, DateTime? visitDate)
    {
        db.Visits.Add(new Visit
        {
            ParkId = parkId,
            UserId = userId,
            VisitDate = visitDate.HasValue
                ? DateTime.SpecifyKind(visitDate.Value, DateTimeKind.Utc)
                : null
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<Visit>> GetVisitsForUserAsync(string userId) =>
        await db.Visits
            .Include(v => v.Park)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.VisitDate ?? v.CreatedAt)
            .ToListAsync();
}
