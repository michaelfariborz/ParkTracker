using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ParkTracker.Components.Shared;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Components;

public class EditVisitModalTests : BunitContext
{
    private const string TestUserId = "user-abc-123";

    private static Park MakePark() =>
        new() { Id = 1, Name = "Yosemite", State = "CA", Latitude = 37.8, Longitude = -119.5 };

    private static Visit MakeVisit(Park park, DateTime? visitDate = null) =>
        new() { Id = 42, ParkId = park.Id, UserId = TestUserId, VisitDate = visitDate };

    private void SetupAuth()
    {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("testuser");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, TestUserId));
    }

    [Fact]
    public void Renders_ParkNameReadOnly_AndPrefilledDate()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park, new DateTime(2024, 7, 4, 0, 0, 0, DateTimeKind.Utc));

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        Assert.Contains("Yosemite (CA)", cut.Markup);
        cut.Find(".form-control-plaintext");
        Assert.Empty(cut.FindAll("select"));
        var dateInput = cut.Find("input[type='date']");
        Assert.Equal("2024-07-04", dateInput.GetAttribute("value"));
    }

    [Fact]
    public async Task UpdateSubmit_CallsUpdateVisitAndFiresCallback()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.UpdateVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>())
            .Returns(true);
        Services.AddSingleton(visitService);

        var callbackFired = false;
        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Factory.Create(this, () => callbackFired = true))
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        await visitService.Received(1).UpdateVisitAsync(42, TestUserId, Arg.Any<DateTime?>());
        Assert.True(callbackFired);
    }

    [Fact]
    public async Task UpdateSubmit_WhenServiceReturnsFalse_ShowsError()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.UpdateVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>())
            .Returns(false);
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        cut.Find(".alert-danger");
    }

    [Fact]
    public async Task UpdateSubmit_WhenServiceThrows_ShowsError()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.UpdateVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>())
            .ThrowsAsync(new Exception("DB error"));
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        var alert = cut.Find(".alert-danger");
        Assert.Contains("Please try again", alert.TextContent);
    }

    [Fact]
    public async Task UpdateSubmit_WhenUserNotAuthenticated_ShowsLoginRequired()
    {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("testuser");
        // No NameIdentifier claim — userId will be null inside HandleSubmit

        var visitService = Substitute.For<IVisitService>();
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find("button[type='submit']").ClickAsync(new());

        var alert = cut.Find(".alert-danger");
        Assert.Contains("logged in", alert.TextContent);
        await visitService.DidNotReceive().UpdateVisitAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task RemoveButton_CallsDeleteVisitAndFiresCallback()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.DeleteVisitAsync(Arg.Any<int>(), Arg.Any<string>())
            .Returns(true);
        Services.AddSingleton(visitService);

        var callbackFired = false;
        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Factory.Create(this, () => callbackFired = true))
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find(".btn-outline-danger").ClickAsync(new());

        await visitService.Received(1).DeleteVisitAsync(42, TestUserId);
        Assert.True(callbackFired);
    }

    [Fact]
    public async Task RemoveButton_WhenServiceReturnsFalse_ShowsError()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.DeleteVisitAsync(Arg.Any<int>(), Arg.Any<string>())
            .Returns(false);
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find(".btn-outline-danger").ClickAsync(new());

        cut.Find(".alert-danger");
    }

    [Fact]
    public async Task RemoveButton_WhenServiceThrows_ShowsError()
    {
        SetupAuth();
        var visitService = Substitute.For<IVisitService>();
        visitService.DeleteVisitAsync(Arg.Any<int>(), Arg.Any<string>())
            .ThrowsAsync(new Exception("DB error"));
        Services.AddSingleton(visitService);

        var park = MakePark();
        var visit = MakeVisit(park);

        var cut = Render<EditVisitModal>(p => p
            .Add(m => m.Visit, visit)
            .Add(m => m.Park, park)
            .Add(m => m.OnVisitSaved, EventCallback.Empty)
            .Add(m => m.OnClose, EventCallback.Empty));

        await cut.Find(".btn-outline-danger").ClickAsync(new());

        var alert = cut.Find(".alert-danger");
        Assert.Contains("Please try again", alert.TextContent);
    }
}
