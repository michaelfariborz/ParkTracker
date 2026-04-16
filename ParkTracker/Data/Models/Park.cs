namespace ParkTracker.Data.Models;

public class Park
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public ICollection<Visit> Visits { get; set; } = [];
}
