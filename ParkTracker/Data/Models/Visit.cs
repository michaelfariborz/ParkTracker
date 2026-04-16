using ParkTracker.Data;

namespace ParkTracker.Data.Models;

public class Visit
{
    public int Id { get; set; }
    public int ParkId { get; set; }
    public Park Park { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public DateTime? VisitDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
