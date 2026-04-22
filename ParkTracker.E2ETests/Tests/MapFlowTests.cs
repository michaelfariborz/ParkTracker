// ParkTracker.E2ETests/Tests/MapFlowTests.cs
using Microsoft.Playwright;
using ParkTracker.E2ETests.Infrastructure;

namespace ParkTracker.E2ETests.Tests;

public class MapFlowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public MapFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HomePageMap_LoadsLeafletContainer()
    {
        var page = await _fixture.NewLoggedInPageAsync("/");
        // Wait for Leaflet to initialize — it adds .leaflet-container
        await page.WaitForSelectorAsync(".leaflet-container", new() { Timeout = 15_000 });
        Assert.True(await page.IsVisibleAsync(".leaflet-container"));
        await page.CloseAsync();
    }

    [Fact]
    public async Task ClickingMapPin_OpensAddVisitModal()
    {
        var page = await _fixture.NewLoggedInPageAsync("/");
        await page.WaitForSelectorAsync(".leaflet-container", new() { Timeout = 15_000 });

        // Wait for at least one Leaflet marker icon to appear
        await page.WaitForSelectorAsync(".leaflet-marker-icon", new() { Timeout = 10_000 });

        // Click the first marker — this opens the Leaflet popup
        var marker = await page.QuerySelectorAsync(".leaflet-marker-icon");
        Assert.NotNull(marker);
        await marker!.ClickAsync();

        // Wait for the popup's "+ Add Visit" button (inside Leaflet popup HTML)
        await page.WaitForSelectorAsync(".leaflet-popup .btn-primary", new() { Timeout = 5_000 });

        // Click the popup button to trigger the JS→.NET [JSInvokable] callback
        await page.ClickAsync(".leaflet-popup .btn-primary");

        // Blazor re-renders with showModal = true
        await page.WaitForSelectorAsync(".modal", new() { Timeout = 5_000 });
        Assert.True(await page.IsVisibleAsync(".modal"));
        await page.CloseAsync();
    }
}
