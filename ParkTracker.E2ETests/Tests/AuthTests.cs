// ParkTracker.E2ETests/Tests/AuthTests.cs
using Microsoft.Playwright;
using ParkTracker.E2ETests.Infrastructure;

namespace ParkTracker.E2ETests.Tests;

public class AuthTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public AuthTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UnauthenticatedNavigation_RedirectsToLogin()
    {
        var page = await _fixture.NewPageAsync("/");
        await page.WaitForURLAsync("**/Account/Login**");
        Assert.Contains("/Account/Login", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        var page = await _fixture.NewPageAsync("/Account/Login");
        await page.FillAsync("input[name='Input.Email']", PlaywrightFixture.AdminEmail);
        await page.FillAsync("input[name='Input.Password']", "WrongPassword999!");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForSelectorAsync(".alert-danger");
        Assert.Contains("Login", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReachesHomePage()
    {
        var page = await _fixture.NewPageAsync("/Account/Login");
        await page.FillAsync("input[name='Input.Email']", PlaywrightFixture.AdminEmail);
        await page.FillAsync("input[name='Input.Password']", PlaywrightFixture.AdminPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync(_fixture.BaseUrl + "/");
        // Blazor Server initializes — wait for the page title
        await page.WaitForSelectorAsync("h1");
        Assert.Equal(_fixture.BaseUrl + "/", page.Url);
        await page.CloseAsync();
    }
}
