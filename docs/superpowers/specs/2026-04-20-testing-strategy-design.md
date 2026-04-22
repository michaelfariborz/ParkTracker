# ParkTracker: Automated Testing Design

## Goal

Add three-layer automated testing with a GitHub Actions gate that blocks Azure deploys when tests fail.

## Architecture

Two new xUnit test projects alongside the main app:

```
ParkTracker/                     ← existing app (.NET 10 Blazor Server)
ParkTracker.Tests/               ← service integration + bUnit component tests
  TestDb.cs                      ← shared SQLite in-memory helper
  Services/
    ParkServiceTests.cs
    VisitServiceTests.cs
  Components/
    AddVisitModalTests.cs
    ParkListTests.cs
    AdminTests.cs
ParkTracker.E2ETests/            ← Playwright browser tests
  Infrastructure/
    TestWebApplicationFactory.cs ← substitutes SQLite DB, dual-host (TestServer + Kestrel)
    PlaywrightFixture.cs         ← IAsyncLifetime, IBrowser, login helper
  Tests/
    AuthTests.cs
    VisitFlowTests.cs
    MapFlowTests.cs
```

`ParkTracker.sln` links all three projects.

## Tech Stack

| Package | Purpose |
|---------|---------|
| xUnit | Test framework for both projects |
| bUnit | Blazor component rendering in tests |
| NSubstitute | Mocking `IParkService` / `IVisitService` |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite in-memory DB for service tests |
| `Microsoft.Data.Sqlite` | Keep `SqliteConnection` alive for in-memory DB lifetime |
| `Microsoft.Playwright` | Browser automation for E2E tests |
| `Microsoft.AspNetCore.Mvc.Testing` | In-process app host for E2E tests |
| `coverlet.collector` | Code coverage collection |

## Changes to the Main Project

### New Interfaces

`IParkService` and `IVisitService` extracted from concrete classes to enable NSubstitute mocking in component tests. Registered as `AddScoped<IParkService, ParkService>()` in `Program.cs`.

### `Program.cs` — Testing Environment

When `IsE2ETesting=true` (set by `TestWebApplicationFactory` via in-memory config), the startup block uses `EnsureCreated()` instead of `MigrateAsync()` and seeds test data directly. This is necessary because EF Core migrations use PostgreSQL-specific SQL that SQLite cannot run.

A `public partial class Program {}` stub at the bottom makes the entry point discoverable by `WebApplicationFactory<Program>`.

### `AddVisitModal.razor` — Error Handling

`HandleSubmit` wraps the service call in try/catch. On failure it logs the exception server-side (via `ILogger<AddVisitModal>`) and shows a generic "Failed to save visit. Please try again." message — not the raw exception message. `saving = false` is in a `finally` block so the button always re-enables.

## `ParkTracker.Tests` — Service & Component Tests

### `TestDb`

Holds a persistent `SqliteConnection("DataSource=:memory:")` and an `ApplicationDbContext`. Because EF Core 10's SQLite provider runs `PRAGMA foreign_keys = ON` automatically, tests that insert `Visit` rows with fake `UserId` strings (no corresponding `AspNetUsers` rows) need FK enforcement off. `TestDb` issues `PRAGMA foreign_keys = OFF` after opening the connection.

`CreateFreshContext()` returns a new `ApplicationDbContext` sharing the same connection (same schema, same data) but with an empty change tracker — required to avoid EF Core's identity cache from returning stale navigation properties.

### Service Tests

**`ParkServiceTests`:**
- `GetAllParksAsync_ReturnsParksSortedByName` — ordering is ascending by name
- `GetAllParksWithUserVisitsAsync_OnlyIncludesRequestingUsersVisits` — filtered Include predicate
- `AddParkAsync_PersistsPark` — saved to DB
- `AddParkAsync_ThrowsOnDuplicateName` — unique index on `Park.Name` enforced

**`VisitServiceTests`:**
- `AddVisitAsync_SpecifiesUtcKind_WhenDateProvided` — `DateTime.Kind == DateTimeKind.Utc`
- `AddVisitAsync_StoresNullDate_WhenNotProvided` — nullable `VisitDate`
- `GetVisitsForUserAsync_OnlyReturnsRequestingUsersVisits` — userId filter
- `GetVisitsForUserAsync_IncludesParkNavigation` — `.Include(v => v.Park)` populated
- `GetVisitsForUserAsync_OrdersByVisitDate_ThenCreatedAt` — descending date ordering

### Component Tests (bUnit)

Each class extends `BunitContext` (bUnit 2.x). Auth state is configured via `this.AddAuthorization()` (bUnit's built-in fake auth). Services are mocked with NSubstitute and registered via `Services.AddSingleton(...)`.

**`AddVisitModalTests`** (5 tests): park dropdown renders, read-only park name when pre-selected, no-selection validation, successful submit fires callback, service exception shows error message.

**`ParkListTests`** (5 tests): renders all parks, search hides non-matches, case-insensitive filter, filter by state, clicking "Add Visit" opens modal.

**`AdminTests`** (3 tests): empty submit shows validation errors, valid submit calls service and shows success, duplicate name error is displayed.

`Home.razor` is excluded — Leaflet JS interop is not meaningfully testable in bUnit; E2E tests cover map interaction instead.

## `ParkTracker.E2ETests` — Playwright Browser Tests

### `TestWebApplicationFactory`

Extends `WebApplicationFactory<Program>`. Removes the Npgsql `ApplicationDbContext` registration (including EF Core's `IDbContextOptionsConfiguration` delegate introduced in EF Core 8+) and substitutes a SQLite file DB in `/tmp` with a unique name per factory instance. Sets `IsE2ETesting=true` and provides `AdminSettings` via in-memory configuration.

Uses a dual-host pattern: builds the standard `TestServer` host (required by `WebApplicationFactory`), then builds a second host with a real Kestrel listener on a dynamic port. Blazor Server's SignalR WebSocket transport requires a real TCP socket — the `TestServer`'s `HttpClient` cannot handle WebSocket upgrades for circuit negotiation.

Cleans up the SQLite file and WAL files in `DisposeAsync`.

### `PlaywrightFixture`

`IAsyncLifetime` used as xUnit class fixture. Calls `Microsoft.Playwright.Program.Main(["install", "chromium"])` to install browsers if absent, starts the factory, captures `BaseUrl`, and creates a shared `IBrowser` instance.

Helpers:
- `NewPageAsync(path)` — fresh browser context (isolated cookies)
- `NewLoggedInPageAsync(path)` — logs in as admin before navigating; waits for the Blazor SignalR negotiate request so `@onclick` handlers are wired up before test interaction

### E2E Tests

**`AuthTests`** (3 tests): unauthenticated redirect to `/Account/Login`, wrong password shows error, valid login reaches home page.

**`VisitFlowTests`** (2 tests): parks table renders with rows, clicking "Add Visit" and submitting shows the `bg-success` visited badge.

**`MapFlowTests`** (2 tests): Leaflet container loads (`.leaflet-container`), clicking a map marker opens the popup then the Add Visit modal.

## CI/CD — GitHub Actions

The workflow splits into two jobs:

**`test` job:**
1. Restore + build Release
2. `pwsh ParkTracker.E2ETests/bin/Release/net10.0/playwright.ps1 install chromium --with-deps` — installs Chromium and OS-level dependencies
3. `dotnet test ParkTracker.sln --configuration Release --no-build` — runs all 8 E2E tests + 22 unit/component tests
4. Upload `.trx` files as artifact (always, even on failure)

**`deploy` job:**
- `needs: test` — only runs if test job succeeds
- Restore, publish `ParkTracker/ParkTracker.csproj` to `./publish`
- Deploy to Azure Web App via `azure/webapps-deploy@v3`

## Verification Checklist

1. `dotnet test ParkTracker.Tests/ParkTracker.Tests.csproj` — 22 tests pass
2. `dotnet test ParkTracker.E2ETests/ParkTracker.E2ETests.csproj` — 8 Playwright tests pass
3. Push to `main` — GitHub Actions runs tests before deploy
4. Intentionally break `VisitService.AddVisitAsync` (remove UTC coercion) → UTC test fails → CI blocked
5. Restore fix → CI green → deploy proceeds
