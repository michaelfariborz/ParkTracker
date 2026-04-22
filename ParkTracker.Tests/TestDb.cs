// ParkTracker.Tests/TestDb.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkTracker.Data;

namespace ParkTracker.Tests;

public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Disable FK enforcement so tests can insert Visits with fake UserId strings
        // without needing real AspNetUsers rows.
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public ApplicationDbContext Context { get; }

    /// <summary>
    /// Creates a fresh DbContext sharing the same SQLite connection. Use this
    /// when passing a context to a service after seeding data, so that the
    /// service starts with an empty change tracker and filtered includes work
    /// correctly without identity-resolution interference from seeded entities.
    /// </summary>
    public ApplicationDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
