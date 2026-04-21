// ParkTracker.E2ETests/Infrastructure/PlaywrightFixture.cs
using Microsoft.Playwright;

namespace ParkTracker.E2ETests.Infrastructure;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public const string AdminEmail = "testadmin@parktracker.test";
    public const string AdminPassword = "TestAdmin1!Pass";

    private IPlaywright? _playwright;
    public IBrowser Browser { get; private set; } = null!;
    public TestWebApplicationFactory Factory { get; } = new();
    public string BaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        // Install Playwright browsers if not already installed
        Microsoft.Playwright.Program.Main(["install", "chromium"]);

        BaseUrl = Factory.EnsureStarted();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright?.Dispose();
        await Factory.DisposeAsync();
    }

    // Helper: open a new page and navigate to a URL
    public async Task<IPage> NewPageAsync(string? path = null)
    {
        var page = await Browser.NewPageAsync();
        if (path is not null)
            await page.GotoAsync($"{BaseUrl}{path}");
        return page;
    }

    // Helper: open a new page already logged in as admin
    public async Task<IPage> NewLoggedInPageAsync(string? path = null)
    {
        var page = await Browser.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        await page.FillAsync("input[name='Input.Email']", AdminEmail);
        await page.FillAsync("input[name='Input.Password']", AdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync(BaseUrl + "/");
        if (path is not null)
            await page.GotoAsync($"{BaseUrl}{path}");
        return page;
    }
}
