// ParkTracker.E2ETests/Infrastructure/TestWebApplicationFactory.cs
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ParkTracker.Data;

namespace ParkTracker.E2ETests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    // Use a temp file so EnsureCreated from the second host startup sees the file exists
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"e2etest_{Guid.NewGuid():N}.db");
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = "";

    public string EnsureStarted()
    {
        if (string.IsNullOrEmpty(ServerAddress))
            _ = Server; // triggers CreateHost which starts both hosts
        return ServerAddress;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development so MapStaticAssets serves blazor.web.js and other framework files.
        // IsE2ETesting flag tells Program.cs to use test seeding instead of migrations.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSettings:Email"] = "testadmin@parktracker.test",
                ["AdminSettings:Password"] = "TestAdmin1!Pass",
                ["IsE2ETesting"] = "true"
            }));

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core DbContext registrations to avoid dual-provider error.
            // Must remove the options AND the options-configuration delegate (EF Core 8+).
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"DataSource={_dbPath}", o => o.CommandTimeout(30)));
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

        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            if (File.Exists(path))
                File.Delete(path);
    }
}
