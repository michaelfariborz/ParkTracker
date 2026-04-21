// ParkTracker.Tests/Components/AddVisitModalTests.cs
using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ParkTracker.Components.Shared;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Components;

public class AddVisitModalTests : BunitContext
{
    private const string TestUserId = "user-abc-123";

    private List<Park> MakeParks() =>
    [
        new Park { Id = 1, Name = "Yosemite", State = "CA", Latitude = 37.8, Longitude = -119.5 },
        new Park { Id = 2, Name = "Zion", State = "UT", Latitude = 37.3, Longitude = -113.0 }
    ];

    private void SetupAuth()
    {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("testuser");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, TestUserId));
    }

    [Fact]
    public void Renders_ParkDropdown_WhenNoPreselectedPark()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        var parks = MakeParks();
        var cut = Render<AddVisitModal>(p => p
            .Add(m => m.Parks, parks)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        var select = cut.Find("select");
        Assert.NotNull(select);
        // Default option + 2 parks
        Assert.Equal(3, cut.FindAll("option").Count);
    }

    [Fact]
    public void Renders_ReadOnlyParkName_WhenPreselectedParkSet()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        var parks = MakeParks();
        var cut = Render<AddVisitModal>(p => p
            .Add(m => m.Parks, parks)
            .Add(m => m.PreselectedParkId, 1)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        cut.Find(".form-control-plaintext");
        Assert.Empty(cut.FindAll("select"));
        Assert.Contains("Yosemite (CA)", cut.Markup);
    }

    [Fact]
    public async Task Submit_WithNoParkSelected_ShowsErrorMessage()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        var parks = MakeParks();
        var cut = Render<AddVisitModal>(p => p
            .Add(m => m.Parks, parks)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        // ParkId defaults to 0 (no selection) — click submit directly
        await cut.Find("button[type='submit']").ClickAsync(new());

        cut.Find(".alert-danger");
        await visitService.DidNotReceive().AddVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task Submit_WithValidPark_CallsAddVisitAndFiresCallback()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.AddVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);
        Services.AddSingleton(visitService);

        var callbackFired = false;
        var parks = MakeParks();
        var cut = Render<AddVisitModal>(p => p
            .Add(m => m.Parks, parks)
            .Add(m => m.PreselectedParkId, 2)
            .Add(m => m.OnVisitSaved, EventCallback.Factory.Create(this, () => callbackFired = true))
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        await visitService.Received(1).AddVisitAsync(2, TestUserId, Arg.Any<DateTime?>());
        Assert.True(callbackFired);
    }

    [Fact]
    public async Task Submit_WhenServiceThrows_ShowsErrorMessage()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.AddVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>())
            .ThrowsAsync(new Exception("DB error"));
        Services.AddSingleton(visitService);

        var parks = MakeParks();
        var cut = Render<AddVisitModal>(p => p
            .Add(m => m.Parks, parks)
            .Add(m => m.PreselectedParkId, 1)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        var alert = cut.Find(".alert-danger");
        Assert.Contains("Please try again", alert.TextContent);
    }
}
