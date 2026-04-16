# ParkTracker — Implementation Plan

## Context
Building a greenfield .NET 10 Blazor Server app to track which US National Parks a user has visited. Multiple users each see their own visit history on an interactive map. Parks are pre-seeded with all ~63 US National Parks. Authentication uses ASP.NET Identity with role-based access (Admin role for park management).

---

## Key Decisions
| Topic | Choice |
|---|---|
| Map | Leaflet.js + OpenStreetMap via custom JS interop (no NuGet wrapper) |
| Auth | ASP.NET Identity (username/password), multi-user |
| Admin access | Admin role only |
| Park data | Pre-seed all ~63 national parks at startup |

---

## Step 1 — Scaffold the Project

```bash
dotnet new blazor -n ParkTracker -o ParkTracker \
  --interactivity Server --auth Individual --all-interactive -f net10.0
cd ParkTracker
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

> The `blazor` template (not `blazorserver`) is correct for .NET 10. `--auth Individual` scaffolds Identity Razor components, `ApplicationUser`, `ApplicationDbContext`, and wires up `Program.cs`. The SQLite package gets swapped for Npgsql.

---

## Step 2 — Database Models

**`Data/Models/ApplicationUser.cs`**
```csharp
public class ApplicationUser : IdentityUser
{
    public ICollection<Visit> Visits { get; set; } = [];
}
```

**`Data/Models/Park.cs`**
```csharp
public class Park
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }
    public ICollection<Visit> Visits { get; set; } = [];
}
```

**`Data/Models/Visit.cs`**
```csharp
public class Visit
{
    public int Id { get; set; }
    public int ParkId { get; set; }
    public Park Park { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public DateTime? VisitDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**`Data/ApplicationDbContext.cs`** — extends `IdentityDbContext<ApplicationUser>`, adds `DbSet<Park>` and `DbSet<Visit>`, configures:
- Cascade deletes on Visit → Park and Visit → User
- `HasDefaultValueSql("NOW()")` on `Visit.CreatedAt`
- Unique index on `Park.Name`

---

## Step 3 — Configure EF Core + Identity in Program.cs

Replace scaffolded SQLite registration:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Identity setup (use `AddIdentityCore`, NOT `AddIdentity` — avoids cookie middleware conflict with Blazor):
```csharp
builder.Services.AddIdentityCore<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
```

**`appsettings.json`** connection string:
```json
"DefaultConnection": "Host=localhost;Database=parktracker;Username=YOUR_USER;Password=YOUR_PASSWORD"
```

---

## Step 4 — Startup Seeding (Program.cs)

After `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()`, before `await app.RunAsync()`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Seed Admin role
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    // Seed Admin user — credentials loaded from config (User Secrets / env vars)
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["AdminSettings:Email"];
    var adminPassword = app.Configuration["AdminSettings:Password"];
    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(adminUser, adminPassword);
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // Seed national parks
    if (!await db.Parks.AnyAsync())
    {
        db.Parks.AddRange(SeedData.GetNationalParks());
        await db.SaveChangesAsync();
    }
}
```

**`Data/SeedData.cs`** — static method returning `List<Park>` with all ~63 national parks (name, state, latitude, longitude).

> Startup seeding (vs `HasData()`) avoids floating-point precision issues in migration files and is idempotent — seeding only runs when the Parks table is empty.

---

## Step 5 — Services

Register as `Scoped` in `Program.cs`.

**`Services/ParkService.cs`**
- `GetAllParksWithUserVisitsAsync(string userId)` — uses filtered Include:
  `.Include(p => p.Visits.Where(v => v.UserId == userId))` to load only the current user's visits
- `GetAllParksAsync()` — for admin screen (no visit filter)
- `AddParkAsync(name, state, lat, lon, description?)` — for admin screen

**`Services/VisitService.cs`**
- `AddVisitAsync(int parkId, string userId, DateTime? visitDate)`
- `GetVisitsForUserAsync(string userId)`

---

## Step 6 — Leaflet JS Interop

**`Components/App.razor`** — add to `<head>`:
```html
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
```
Add before `</body>`:
```html
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script src="js/leafletInterop.js"></script>
```

> `App.razor` is the HTML shell in .NET 10 Blazor (replaces `_Host.cshtml`). Scripts go here, not in page `@section` blocks.

**`wwwroot/js/leafletInterop.js`** — IIFE exposing `window.leafletInterop` with:
- `initMap(containerId, lat, lon, zoom)` — creates the Leaflet map; calls `map.remove()` first if already initialized (prevents "Map container already initialized" error on re-render)
- `addPins(parks)` — parks is an array of `{ id, name, state, latitude, longitude, visited, visitDates[] }`. Red `L.divIcon` circle for visited, gray for unvisited. Popups show name + state for all parks; visited parks also list all visit dates.
- `clearPins()` — removes all markers without destroying the map (preserves zoom/pan state)

**C# call pattern in `Home.razor`** — always call JS from `OnAfterRenderAsync(firstRender)`, never `OnInitializedAsync` (DOM doesn't exist yet):
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender) await InitializeMap();
}
```

After a visit is saved: call `clearPins` then `addPins` again to refresh marker colors without resetting the map view.

Get current user ID via injected `AuthenticationStateProvider` (do not use `HttpContext` in Blazor Server components):
```csharp
var authState = await AuthStateProvider.GetAuthenticationStateAsync();
currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

---

## Step 7 — Pages and Components

**`Components/Pages/Home.razor`**
- `@attribute [Authorize]`, `@rendermode InteractiveServer`
- `<div id="map-container" style="height:600px;width:100%"></div>`
- "Add Visit" button → toggles `showModal`
- `<AddVisitModal>` rendered conditionally, receives parks list and `OnVisitSaved` callback

**`Components/Shared/AddVisitModal.razor`**
- Parameters: `List<Park> Parks`, `EventCallback OnVisitSaved`, `EventCallback OnClose`
- `EditForm` with `InputSelect` (park dropdown, ordered by name) and `InputDate` (optional)
- On submit: calls `VisitService.AddVisitAsync(...)`, then invokes `OnVisitSaved`

**`Components/Pages/Admin.razor`**
- `@attribute [Authorize(Roles = "Admin")]`, `@rendermode InteractiveServer`
- Table of all parks from `ParkService.GetAllParksAsync()`
- `EditForm` to add a new park (name, state, lat, lon)

**Identity pages** (`Components/Account/Pages/Login.razor`, `Register.razor`) — generated by scaffold, no significant changes needed. Both are `[AllowAnonymous]`.

**`Routes.razor`** — `AuthorizeRouteView` with `<NotAuthorized>` handler that redirects unauthenticated users to login and shows a "not authorized" message for authenticated non-admin users.

---

## Step 8 — Create Migration and Run

```bash
# Create the Postgres database (one-time)
psql -U postgres -c "CREATE DATABASE parktracker;"

# Create EF migration
dotnet ef migrations add InitialCreate

# Run the app — MigrateAsync() applies migrations + seeds on startup
dotnet run
```

First run automatically: creates all tables, seeds Admin role, seeds admin user (credentials from `AdminSettings:Email` / `AdminSettings:Password` config), seeds all 63 national parks.

---

## Project Structure

```
ParkTracker/
├── Program.cs
├── appsettings.json
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── SeedData.cs
│   └── Models/
│       ├── ApplicationUser.cs
│       ├── Park.cs
│       └── Visit.cs
├── Services/
│   ├── ParkService.cs
│   └── VisitService.cs
├── Components/
│   ├── App.razor                  ← Leaflet CDN links here
│   ├── Routes.razor
│   ├── Layout/
│   ├── Pages/
│   │   ├── Home.razor
│   │   └── Admin.razor
│   ├── Account/Pages/             ← Scaffolded Identity UI
│   └── Shared/
│       └── AddVisitModal.razor
└── wwwroot/
    └── js/
        └── leafletInterop.js
```

---

## Verification Checklist

1. `dotnet run` — app starts, migrations apply, parks seeded (check console output)
2. Navigate to `/` — redirected to login page
3. Register a new user → logged in → map loads showing ~63 gray pins across the US
4. Click "Add Visit", select a park, optionally enter a date, save → that pin turns red; clicking it shows the visit date in a popup
5. Log out, log in as your configured admin credentials → `/admin` accessible, shows park table, can add a new park
6. Log in as regular user → navigate to `/admin` → "not authorized" message shown

---

## Gotchas

- Use `OnAfterRenderAsync(firstRender)` for JS interop — DOM doesn't exist in `OnInitializedAsync`
- Use `AddIdentityCore` + `AddSignInManager`, not `AddIdentity<>` — the latter conflicts with Blazor's auth cookie setup
- Npgsql connection string format: `Host=...;Database=...;Username=...;Password=...` (not SQL Server syntax)
- Leaflet CDN goes in `App.razor` (the HTML shell), not in individual page components
- Call `map.remove()` before re-initializing Leaflet on the same container element
- `AddRoles<IdentityRole>()` must be chained before `.AddEntityFrameworkStores<>()`
