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

    // Open a new isolated browser context (fresh cookies) and navigate to a URL
    public async Task<IPage> NewPageAsync(string? path = null)
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        if (path is not null)
            await page.GotoAsync($"{BaseUrl}{path}");
        return page;
    }

    // Open a new isolated context, log in as admin, then navigate to path
    public async Task<IPage> NewLoggedInPageAsync(string? path = null)
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        await page.FillAsync("input[name='Input.Email']", AdminEmail);
        await page.FillAsync("input[name='Input.Password']", AdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync(BaseUrl + "/");
        if (path is not null)
            await NavigateAndAwaitBlazorCircuitAsync(page, $"{BaseUrl}{path}");
        return page;
    }

    // Navigate to url and wait for Blazor's interactive render to finish so that
    // @onclick handlers are wired before the caller interacts with the page.
    //
    // Blazor Server SSR-renders the initial HTML (table rows, buttons) before the
    // SignalR circuit connects. For the first page load, a new negotiate + WebSocket
    // establishes the circuit. For subsequent pages via enhanced navigation, the
    // existing circuit is reused — no new negotiate, no new WebSocket event fires.
    //
    // The reliable cross-case signal is DOM stability: once Blazor's interactive
    // render diff has been applied, the DOM stops mutating. We wait 400 ms of quiet.
    private static async Task NavigateAndAwaitBlazorCircuitAsync(IPage page, string url)
    {
        var negotiate = page.WaitForResponseAsync(
            r => r.Url.Contains("/_blazor/negotiate"),
            new() { Timeout = 10_000 });
        await page.GotoAsync(url);
        try { await negotiate; }
        catch { /* enhanced navigation — existing circuit, no new negotiate */ }

        // Wait for DOM to stop mutating: Blazor's interactive render diff is done.
        try
        {
            await page.WaitForFunctionAsync(
                @"new Promise(resolve => {
                    let t = setTimeout(resolve, 400);
                    const obs = new MutationObserver(() => { clearTimeout(t); t = setTimeout(resolve, 400); });
                    obs.observe(document.body, { childList: true, subtree: true, characterData: true });
                })",
                new PageWaitForFunctionOptions { Timeout = 8_000 });
        }
        catch { /* best effort */ }
    }
}
