// ParkTracker.Tests/Components/AdminTests.cs
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ParkTracker.Components.Pages;
using ParkTracker.Data.Models;
using ParkTracker.Services;

namespace ParkTracker.Tests.Components;

public class AdminTests : BunitContext
{
    private IParkService SetupServices(List<Park>? initialParks = null)
    {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var parkService = Substitute.For<IParkService>();
        parkService.GetAllParksAsync()
            .Returns(Task.FromResult(initialParks ?? []));
        Services.AddSingleton(parkService);
        return parkService;
    }

    [Fact]
    public async Task EmptyFormSubmit_ShowsValidationErrors()
    {
        SetupServices();
        var cut = Render<Admin>();
        await cut.InvokeAsync(() => { });

        // Click submit without filling required fields
        await cut.Find("button[type='submit']").ClickAsync(new());

        // DataAnnotationsValidator fires Required attribute errors
        var validationMessages = cut.FindAll(".validation-message");
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task ValidSubmit_CallsAddParkAsync_AndShowsSuccess()
    {
        var parkService = SetupServices();
        parkService.AddParkAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<string?>())
            .Returns(Task.CompletedTask);
        // Return updated list after adding (called twice: once on init, once after add)
        parkService.GetAllParksAsync()
            .Returns(
                Task.FromResult(new List<Park>()),
                Task.FromResult(new List<Park>
                {
                    new() { Id = 1, Name = "Olympic", State = "WA", Latitude = 47.9, Longitude = -123.5 }
                }));

        var cut = Render<Admin>();
        await cut.InvokeAsync(() => { });

        // Re-issue FindAll after render to avoid stale element references
        await cut.InvokeAsync(() =>
        {
            cut.FindAll("input.form-control")[0].Change("Olympic");
            cut.FindAll("input.form-control")[1].Change("WA");
        });

        await cut.Find("button[type='submit']").ClickAsync(new());

        await parkService.Received(1).AddParkAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<string?>());

        cut.Find(".alert-success");
    }

    [Fact]
    public async Task ValidSubmit_WhenDuplicateName_ShowsErrorMessage()
    {
        var parkService = SetupServices();
        parkService.AddParkAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<string?>())
            .ThrowsAsync(new Exception("UNIQUE constraint failed"));

        var cut = Render<Admin>();
        await cut.InvokeAsync(() => { });

        // Re-issue FindAll after render to avoid stale element references
        await cut.InvokeAsync(() =>
        {
            cut.FindAll("input.form-control")[0].Change("Yellowstone");
            cut.FindAll("input.form-control")[1].Change("WY");
        });

        await cut.Find("button[type='submit']").ClickAsync(new());

        var alert = cut.Find(".alert-danger");
        Assert.Contains("UNIQUE constraint failed", alert.TextContent);
    }

    [Fact]
    public async Task ValidSubmit_ClearsFormFieldsAfterSuccess()
    {
        var parkService = SetupServices();
        parkService.AddParkAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<double>(), Arg.Any<double>(),
            Arg.Any<string?>())
            .Returns(Task.CompletedTask);
        parkService.GetAllParksAsync()
            .Returns(Task.FromResult(new List<Park>()));

        var cut = Render<Admin>();
        await cut.InvokeAsync(() => { });

        await cut.InvokeAsync(() =>
        {
            cut.FindAll("input.form-control")[0].Change("Olympic");
            cut.FindAll("input.form-control")[1].Change("WA");
        });

        await cut.Find("button[type='submit']").ClickAsync(new());

        // After submit, model = new() — Name and State inputs should be empty
        var inputs = cut.FindAll("input.form-control");
        Assert.Equal("", inputs[0].GetAttribute("value") ?? "");
        Assert.Equal("", inputs[1].GetAttribute("value") ?? "");
    }
}
