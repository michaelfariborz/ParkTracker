// ParkTracker.E2ETests/Tests/VisitFlowTests.cs
using Microsoft.Playwright;
using ParkTracker.E2ETests.Infrastructure;

namespace ParkTracker.E2ETests.Tests;

public class VisitFlowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public VisitFlowTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ParkListPage_ShowsParks()
    {
        var page = await _fixture.NewLoggedInPageAsync("/parks");
        await page.WaitForSelectorAsync("table");
        var rows = await page.QuerySelectorAllAsync("tbody tr");
        Assert.NotEmpty(rows);
        await page.CloseAsync();
    }

    [Fact]
    public async Task LoggingAVisit_UpdatesParkStatusToVisited()
    {
        var page = await _fixture.NewLoggedInPageAsync("/parks");
        await page.WaitForSelectorAsync("table");

        // Use Locator (lazy, re-queries on each action) to avoid stale element handles
        // after Blazor re-renders the component post-circuit-init.
        var firstAddVisit = page.Locator("button:has-text('Add Visit')").First;
        await firstAddVisit.WaitForAsync();
        await firstAddVisit.ClickAsync();

        // Wait for modal
        await page.WaitForSelectorAsync(".modal");

        // Submit without a date (date is optional).
        // Scope to .modal to avoid matching the Logout button in the nav header.
        await page.Locator(".modal button[type='submit']").ClickAsync();

        // Modal should close
        await page.WaitForSelectorAsync(".modal", new() { State = WaitForSelectorState.Hidden });

        // Park should now show "Visited" badge
        await page.WaitForSelectorAsync(".badge.bg-success");
        Assert.True(await page.IsVisibleAsync(".badge.bg-success"));
        await page.CloseAsync();
    }
}
