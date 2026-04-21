// ParkTracker.Tests/Services/ParkServiceTests.cs
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Services;

public class ParkServiceTests
{
    [Fact]
    public async Task GetAllParksAsync_ReturnsParksSortedByName()
    {
        using var db = new TestDb();
        db.Context.Parks.AddRange(
            new Park { Name = "Zion", State = "UT", Latitude = 37.3, Longitude = -113.0 },
            new Park { Name = "Acadia", State = "ME", Latitude = 44.4, Longitude = -68.2 },
            new Park { Name = "Mesa Verde", State = "CO", Latitude = 37.2, Longitude = -108.5 }
        );
        await db.Context.SaveChangesAsync();

        var service = new ParkService(db.Context);
        var parks = await service.GetAllParksAsync();

        Assert.Equal(3, parks.Count);
        Assert.Equal("Acadia", parks[0].Name);
        Assert.Equal("Mesa Verde", parks[1].Name);
        Assert.Equal("Zion", parks[2].Name);
    }

    [Fact]
    public async Task GetAllParksWithUserVisitsAsync_OnlyIncludesRequestingUsersVisits()
    {
        using var db = new TestDb();

        var park = new Park { Name = "Yosemite", State = "CA", Latitude = 37.8, Longitude = -119.5 };
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        // User 1 visits the park
        db.Context.Visits.Add(new Visit { ParkId = park.Id, UserId = "user-1", VisitDate = null });
        // User 2 also visits the park
        db.Context.Visits.Add(new Visit { ParkId = park.Id, UserId = "user-2", VisitDate = null });
        await db.Context.SaveChangesAsync();

        // Use a fresh context so the service's filtered Include isn't affected by
        // entities already tracked from the seeding step above.
        using var serviceCtx = db.CreateFreshContext();
        var service = new ParkService(serviceCtx);
        var parks = await service.GetAllParksWithUserVisitsAsync("user-1");

        Assert.Single(parks);
        Assert.Single(parks[0].Visits);
        Assert.Equal("user-1", parks[0].Visits.First().UserId);
    }

    [Fact]
    public async Task AddParkAsync_PersistsPark()
    {
        using var db = new TestDb();
        var service = new ParkService(db.Context);

        await service.AddParkAsync("Grand Canyon", "AZ", 36.1, -112.1, "A big hole");

        var parks = await service.GetAllParksAsync();
        Assert.Single(parks);
        Assert.Equal("Grand Canyon", parks[0].Name);
        Assert.Equal("AZ", parks[0].State);
        Assert.Equal("A big hole", parks[0].Description);
    }

    [Fact]
    public async Task AddParkAsync_ThrowsOnDuplicateName()
    {
        using var db = new TestDb();
        var service = new ParkService(db.Context);

        await service.AddParkAsync("Yellowstone", "WY", 44.4, -110.5);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.AddParkAsync("Yellowstone", "WY", 44.4, -110.5));
    }
}
