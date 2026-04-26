using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ParkTracker.Components.Pages;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Components;

public class HomeTests : BunitContext
{
    private const string TestUserId = "user-123";

    private static Park MakePark(int id, string name, bool visited = false) =>
        new()
        {
            Id = id, Name = name, State = "CA", Latitude = 37.0, Longitude = -119.0,
            Visits = visited
                ? [new Visit { ParkId = id, UserId = TestUserId, CreatedAt = DateTime.UtcNow }]
                : []
        };

    private void SetupServices(List<Park> parks)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var auth = this.AddAuthorization();
        auth.SetAuthorized("testuser");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, TestUserId));

        var parkService = Substitute.For<IParkService>();
        parkService.GetAllParksWithUserVisitsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(parks));
        Services.AddSingleton(parkService);

        Services.AddSingleton(Substitute.For<IVisitService>());
    }

    [Fact]
    public async Task HeroCounter_ShowsCorrectVisitedCountAndTotal()
    {
        SetupServices([MakePark(1, "Acadia", visited: true), MakePark(2, "Zion")]);
        var cut = Render<Home>();
        await cut.InvokeAsync(() => { });

        Assert.Equal("1", cut.Find(".visit-stats__count").TextContent);
        Assert.Contains("2", cut.Find(".visit-stats__total").TextContent);
    }

    [Fact]
    public async Task ProgressBar_ReflectsCorrectPercentage()
    {
        SetupServices([MakePark(1, "Acadia", visited: true), MakePark(2, "Zion")]);
        var cut = Render<Home>();
        await cut.InvokeAsync(() => { });

        Assert.Contains("50%", cut.Find(".visit-stats__fill").GetAttribute("style") ?? "");
        Assert.Contains("50%", cut.Find(".visit-stats__label").TextContent);
    }

    [Fact]
    public async Task ProgressBar_WhenNoParks_ShowsZeroPercent()
    {
        SetupServices([]);
        var cut = Render<Home>();
        await cut.InvokeAsync(() => { });

        Assert.Contains("0%", cut.Find(".visit-stats__fill").GetAttribute("style") ?? "");
        Assert.Contains("0%", cut.Find(".visit-stats__label").TextContent);
    }

    [Fact]
    public async Task ProgressBar_WhenAllParksVisited_ShowsHundredPercent()
    {
        SetupServices([MakePark(1, "Acadia", visited: true), MakePark(2, "Zion", visited: true)]);
        var cut = Render<Home>();
        await cut.InvokeAsync(() => { });

        Assert.Contains("100%", cut.Find(".visit-stats__fill").GetAttribute("style") ?? "");
        Assert.Contains("100%", cut.Find(".visit-stats__label").TextContent);
    }
}
