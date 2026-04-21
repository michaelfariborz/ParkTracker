// ParkTracker.E2ETests/Infrastructure/TestWebApplicationFactory.cs
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ParkTracker.Data;

namespace ParkTracker.E2ETests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Shared database name so both hosts access the same SQLite in-memory DB
    private readonly string _dbName = $"e2etest_{Guid.NewGuid():N}";
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = "";

    // Call this to start the Kestrel host and get the base URL for Playwright
    public string EnsureStarted()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            _ = Server; // triggers CreateHost which starts both hosts
        return ServerAddress;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSettings:Email"] = "testadmin@parktracker.test",
                // 15 chars, uppercase, lowercase, digit, special — meets the 12+ char policy
                ["AdminSettings:Password"] = "TestAdmin1!Pass"
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the Npgsql DbContext registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Register SQLite with Cache=Shared so both hosts share the same in-memory DB
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    $"DataSource={_dbName};Mode=Memory;Cache=Shared",
                    o => o.CommandTimeout(30)));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the in-process TestServer host (required by WebApplicationFactory)
        var testHost = builder.Build();

        // Build a separate host with a real Kestrel TCP listener for Playwright
        builder.ConfigureWebHost(b =>
            b.UseKestrel(o => o.Listen(IPAddress.Loopback, 0)));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses;
        ServerAddress = addresses.First();

        testHost.Start();
        return testHost;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_kestrelHost is not null)
        {
            await _kestrelHost.StopAsync();
            _kestrelHost.Dispose();
        }
        await base.DisposeAsync();
    }
}
