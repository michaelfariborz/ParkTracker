// ParkTracker.Tests/Components/ParkListTests.cs
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ParkTracker.Components.Pages;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Components;

public class ParkListTests : BunitContext
{
    private const string TestUserId = "user-123";

    private static List<Park> MakeParks() =>
    [
        new Park { Id = 1, Name = "Acadia", State = "ME", Latitude = 44.4, Longitude = -68.2 },
        new Park { Id = 2, Name = "Zion", State = "UT", Latitude = 37.3, Longitude = -113.0 }
    ];

    private IParkService SetupServices(List<Park>? parks = null)
    {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("testuser");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, TestUserId));

        var parkService = Substitute.For<IParkService>();
        parkService.GetAllParksWithUserVisitsAsync(Arg.Any<string>())
            .Returns(Task.FromResult(parks ?? MakeParks()));
        Services.AddSingleton(parkService);

        // AddVisitModal (rendered inside ParkList when modal opens) also needs IVisitService
        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        return parkService;
    }

    [Fact]
    public async Task RendersAllParksInTable()
    {
        SetupServices();
        var cut = Render<ParkList>();
        await cut.InvokeAsync(() => { }); // wait for OnInitializedAsync

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Acadia", cut.Markup);
        Assert.Contains("Zion", cut.Markup);
    }

    [Fact]
    public async Task FilterText_HidesNonMatchingParks()
    {
        SetupServices();
        var cut = Render<ParkList>();
        await cut.InvokeAsync(() => { });

        // @bind:event="oninput" — must trigger the input event, not onchange
        await cut.Find("input[placeholder*='Search']")
            .TriggerEventAsync("oninput", new ChangeEventArgs { Value = "acad" });
        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        var rows = cut.FindAll("tbody tr");
        Assert.Single(rows);
        Assert.Contains("Acadia", rows[0].TextContent);
    }

    [Fact]
    public async Task FilterText_IsCaseInsensitive()
    {
        SetupServices();
        var cut = Render<ParkList>();
        await cut.InvokeAsync(() => { });

        await cut.Find("input[placeholder*='Search']")
            .TriggerEventAsync("oninput", new ChangeEventArgs { Value = "ZION" });
        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        Assert.Contains("Zion", cut.FindAll("tbody tr")[0].TextContent);
    }

    [Fact]
    public async Task FilterText_ByState_FiltersCorrectly()
    {
        SetupServices();
        var cut = Render<ParkList>();
        await cut.InvokeAsync(() => { });

        await cut.Find("input[placeholder*='Search']")
            .TriggerEventAsync("oninput", new ChangeEventArgs { Value = "UT" });
        cut.WaitForState(() => cut.FindAll("tbody tr").Count == 1);

        Assert.Contains("Zion", cut.FindAll("tbody tr")[0].TextContent);
    }

    [Fact]
    public async Task ClickingAddVisit_OpensModal()
    {
        SetupServices();
        var cut = Render<ParkList>();
        await cut.InvokeAsync(() => { });

        var addVisitButton = cut.FindAll("button").First(b => b.TextContent.Contains("Add Visit"));
        await addVisitButton.ClickAsync(new());

        Assert.NotEmpty(cut.FindAll(".modal"));
    }
}
