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

        // Click the first "Add Visit" button
        var addButtons = await page.QuerySelectorAllAsync("button:has-text('Add Visit')");
        Assert.NotEmpty(addButtons);
        await addButtons[0].ClickAsync();

        // Wait for modal
        await page.WaitForSelectorAsync(".modal");

        // Submit without a date (date is optional)
        await page.ClickAsync("button[type='submit']");

        // Modal should close
        await page.WaitForSelectorAsync(".modal", new() { State = WaitForSelectorState.Hidden });

        // Park should now show "Visited" badge
        await page.WaitForSelectorAsync(".badge.bg-success");
        Assert.True(await page.IsVisibleAsync(".badge.bg-success"));
        await page.CloseAsync();
    }
}
