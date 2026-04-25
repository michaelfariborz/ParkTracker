using Microsoft.EntityFrameworkCore;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Services;

public class VisitServiceTests
{
    private static Park MakePark(string name = "Test Park") =>
        new() { Name = name, State = "CA", Latitude = 37.0, Longitude = -120.0 };

    [Fact]
    public async Task AddVisitAsync_SpecifiesUtcKind_WhenDateProvided()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var inputDate = new DateTime(2024, 7, 4, 0, 0, 0, DateTimeKind.Unspecified);
        await service.AddVisitAsync(park.Id, "user-1", inputDate);

        var visit = db.Context.Visits.Single();
        Assert.NotNull(visit.VisitDate);
        Assert.Equal(DateTimeKind.Utc, visit.VisitDate!.Value.Kind);
        Assert.Equal(new DateTime(2024, 7, 4, 0, 0, 0, DateTimeKind.Utc), visit.VisitDate.Value);
    }

    [Fact]
    public async Task AddVisitAsync_StoresNullDate_WhenNotProvided()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        await service.AddVisitAsync(park.Id, "user-1", visitDate: null);

        var visit = db.Context.Visits.Single();
        Assert.Null(visit.VisitDate);
    }

    [Fact]
    public async Task GetVisitsForUserAsync_OnlyReturnsRequestingUsersVisits()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        db.Context.Visits.AddRange(
            new Visit { ParkId = park.Id, UserId = "user-1" },
            new Visit { ParkId = park.Id, UserId = "user-2" }
        );
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var visits = await service.GetVisitsForUserAsync("user-1");

        Assert.Single(visits);
        Assert.Equal("user-1", visits[0].UserId);
    }

    [Fact]
    public async Task GetVisitsForUserAsync_IncludesParkNavigation()
    {
        using var db = new TestDb();
        var park = MakePark("Sequoia");
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        db.Context.Visits.Add(new Visit { ParkId = park.Id, UserId = "user-1" });
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var visits = await service.GetVisitsForUserAsync("user-1");

        Assert.NotNull(visits[0].Park);
        Assert.Equal("Sequoia", visits[0].Park.Name);
    }

    [Fact]
    public async Task GetVisitsForUserAsync_OrdersByVisitDate_ThenCreatedAt()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        await db.Context.SaveChangesAsync();

        var older = new Visit
        {
            ParkId = park.Id,
            UserId = "user-1",
            VisitDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var newer = new Visit
        {
            ParkId = park.Id,
            UserId = "user-1",
            VisitDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Context.Visits.AddRange(older, newer);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var visits = await service.GetVisitsForUserAsync("user-1");

        // OrderByDescending means newest first
        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc), visits[0].VisitDate);
        Assert.Equal(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), visits[1].VisitDate);
    }

    [Fact]
    public async Task UpdateVisitAsync_UpdatesDateToUtc_WhenDateProvided()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        var visit = new Visit { ParkId = park.Id, UserId = "user-1", VisitDate = null };
        db.Context.Visits.Add(visit);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var result = await service.UpdateVisitAsync(visit.Id, "user-1", new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Unspecified));

        Assert.True(result);
        var updated = db.Context.Visits.Single();
        Assert.NotNull(updated.VisitDate);
        Assert.Equal(DateTimeKind.Utc, updated.VisitDate!.Value.Kind);
        Assert.Equal(new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc), updated.VisitDate.Value);
    }

    [Fact]
    public async Task UpdateVisitAsync_ClearsDate_WhenNullProvided()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        var visit = new Visit { ParkId = park.Id, UserId = "user-1", VisitDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        db.Context.Visits.Add(visit);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.Context);
        var result = await service.UpdateVisitAsync(visit.Id, "user-1", visitDate: null);

        Assert.True(result);
        Assert.Null(db.Context.Visits.Single().VisitDate);
    }

    [Fact]
    public async Task UpdateVisitAsync_ReturnsFalse_WhenVisitNotFound()
    {
        using var db = new TestDb();
        var service = new VisitService(db.Context);

        var result = await service.UpdateVisitAsync(visitId: 999, "user-1", visitDate: null);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateVisitAsync_ReturnsFalse_WhenWrongUser()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        var visit = new Visit { ParkId = park.Id, UserId = "user-1" };
        db.Context.Visits.Add(visit);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.CreateFreshContext());
        var result = await service.UpdateVisitAsync(visit.Id, "user-2", visitDate: null);

        Assert.False(result);
        Assert.NotNull(db.Context.Visits.Find(visit.Id)); // unchanged
    }

    [Fact]
    public async Task DeleteVisitAsync_RemovesVisit_WhenOwner()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        var visit = new Visit { ParkId = park.Id, UserId = "user-1" };
        db.Context.Visits.Add(visit);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.CreateFreshContext());
        var result = await service.DeleteVisitAsync(visit.Id, "user-1");

        Assert.True(result);
        Assert.Empty(db.Context.Visits);
    }

    [Fact]
    public async Task DeleteVisitAsync_ReturnsFalse_WhenVisitNotFound()
    {
        using var db = new TestDb();
        var service = new VisitService(db.Context);

        var result = await service.DeleteVisitAsync(visitId: 999, "user-1");

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteVisitAsync_ReturnsFalse_WhenWrongUser()
    {
        using var db = new TestDb();
        var park = MakePark();
        db.Context.Parks.Add(park);
        var visit = new Visit { ParkId = park.Id, UserId = "user-1" };
        db.Context.Visits.Add(visit);
        await db.Context.SaveChangesAsync();

        var service = new VisitService(db.CreateFreshContext());
        var result = await service.DeleteVisitAsync(visit.Id, "user-2");

        Assert.False(result);
        Assert.NotNull(db.Context.Visits.Find(visit.Id)); // unchanged
    }
}
