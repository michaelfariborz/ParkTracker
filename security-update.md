# ParkTracker Pre-Deployment Security Review

## Summary

The application has a solid security foundation (EF Core parameterized queries, CSRF protection, HTTPS enforcement, role-based authorization, no hardcoded secrets), but several issues must be resolved before deploying to Azure.

---

## 🔴 CRITICAL — Fix Before Any Deployment

### 1. Account Lockout Disabled — Brute Force Vulnerability
**File:** `ParkTracker/Components/Account/Pages/Login.razor:124`

Unlimited password guesses are allowed because lockout is explicitly disabled.

```csharp
// CURRENT (vulnerable):
result = await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

// FIX:
result = await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
```

---

### 2. No Password Complexity Requirements
**File:** `ParkTracker/Program.cs:31`

`AddIdentityCore` is called with no password policy. Default minimum is only 6 characters with no complexity rules.

```csharp
// Add inside AddIdentityCore options lambda:
options.Password.RequiredLength = 12;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequireLowercase = true;
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

---

### 3. XSS in Leaflet Popup — Unescaped HTML
**File:** `ParkTracker/wwwroot/js/leafletInterop.js:36-43`

Park names and states are inserted into HTML template literals without HTML-escaping. A malicious park name (entered by a compromised admin or via DB injection) would execute as JavaScript in every user's browser.

```javascript
// CURRENT (vulnerable):
let popupHtml = `<strong>${park.name}</strong><br>${park.state}`;

// FIX — add helper function and use it:
function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
let popupHtml = `<strong>${escapeHtml(park.name)}</strong><br>${escapeHtml(park.state)}`;
```

---

### 4. Open Redirect on Logout
**File:** `ParkTracker/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs:44-51`

`returnUrl` from the POST body is passed directly to `LocalRedirect` without validation. An attacker can craft a logout link that redirects to a phishing site.

```csharp
// CURRENT (vulnerable):
return TypedResults.LocalRedirect($"~/{returnUrl}");

// FIX — validate before redirecting (same pattern as IdentityRedirectManager.cs:24-27):
if (string.IsNullOrEmpty(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    returnUrl = "~/";
return TypedResults.LocalRedirect($"~/{returnUrl}");
```

---

### 5. No Functional Email Sender — Password Reset Broken
**File:** `ParkTracker/Components/Account/IdentityNoOpEmailSender.cs`

The `NoOpEmailSender` silently discards all emails. Password reset and email confirmation links are never delivered — users cannot recover lost accounts.

**Fix:** Implement a real email provider before launch (Azure Communication Services or SendGrid). Store the API key in Azure Key Vault and inject via configuration.

---

## 🟠 HIGH — Address Before Public Launch

### 6. Missing Security HTTP Headers
**File:** `ParkTracker/Program.cs` (no security headers middleware present)

Missing headers expose the app to clickjacking, MIME-sniffing, and increase XSS impact.

```csharp
// Add this middleware block before app.UseHsts():
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "img-src 'self' data: https://*.tile.openstreetmap.org;";
    await next();
});
```

> Note: `unsafe-inline` is required for Blazor Server's inline scripts and Leaflet. A nonce-based CSP would be needed to eliminate it.

---

### 7. AllowedHosts Wildcard
**File:** `ParkTracker/appsettings.json:15`

```json
"AllowedHosts": "*"
```

**Fix:** Override this in Azure Application Settings (not in appsettings.json, which is checked into source control):

```
AllowedHosts = parktracker-app.azurewebsites.net
```

---

### 8. Admin Credentials Visible in Azure Portal
**File:** `azure-setup.md:76-79`

`AdminSettings__Email` and `AdminSettings__Password` stored as plain Azure Application Settings are visible in plaintext to anyone with portal access.

**Fix:** Store both values in Azure Key Vault and reference them using Key Vault references in App Service configuration:
```
@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/AdminEmail/)
```

---

## 🟡 MEDIUM — Address Soon After Launch

### 9. Email Confirmation Disabled
**File:** `ParkTracker/Program.cs:31`

`RequireConfirmedAccount = false` allows registration with any email address, including non-existent ones. Change to `true` after implementing a real email sender (fix #5).

### 10. Username Enumeration via Passkey Endpoint
**File:** `ParkTracker/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs:78-90`

The `/Account/PasskeyRequestOptions?username=foo` endpoint allows timing-based enumeration of valid usernames. Add rate limiting and ensure consistent response timing.

### 11. No Rate Limiting on Authentication Endpoints
No rate limiting on login, registration, or password reset endpoints (lockout fix #1 partially mitigates login, but not others).

**Fix:**
```csharp
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
    }));
// Apply policy to auth route group
```

---

## 🔵 LOW — Good Practice Improvements

| Issue | File | Notes |
|-------|------|-------|
| Leaflet loaded from CDN | `Components/App.razor:15,20` | Self-host in `wwwroot` to eliminate third-party CDN dependency |
| No Dependabot | `.github/` | Add `dependabot.yml` for automated NuGet vulnerability scanning |
| No tests in CI/CD | `.github/workflows/azure-deploy.yml` | Add `dotnet test` step before deployment |
| HSTS max-age is 30 days | `Program.cs:53` | Increase to 1 year (`31536000`) once HTTPS config is stable |

---

## Confirmed Strengths

- **SQL Injection:** EF Core ORM with parameterized queries throughout — no raw SQL
- **CSRF:** `UseAntiforgery()` globally enabled; validated on all form posts and passkey endpoints
- **HTTPS:** `UseHttpsRedirection()` + HSTS enabled
- **Session integrity:** Security stamp revalidated every 30 min (`IdentityRevalidatingAuthenticationStateProvider.cs:18`)
- **Authorization:** `[Authorize(Roles = "Admin")]` on Admin page; `[Authorize]` on Home page
- **Secrets:** No hardcoded credentials anywhere in source
- **Error handling:** Stack traces not shown to users in production (`Error.razor`)
- **Open redirect guard:** Pattern exists in `IdentityRedirectManager.cs:24-27` (reuse for fix #4)

---

## Recommended Fix Order

| Priority | Change | File | Effort |
|----------|--------|------|--------|
| 1 | Enable account lockout | `Login.razor:124` | 1 line |
| 2 | Add password policy + lockout config | `Program.cs:31` | ~10 lines |
| 3 | HTML-escape Leaflet popup data | `leafletInterop.js:36-43` | ~10 lines |
| 4 | Validate returnUrl on logout | `IdentityComponentsEndpointRouteBuilderExtensions.cs:44-51` | ~3 lines |
| 5 | Add security headers middleware | `Program.cs` | ~10 lines |
| 6 | Restrict AllowedHosts | Azure App Settings | Config only |
| 7 | Migrate secrets to Key Vault | Azure Portal | Infra change |
| 8 | Implement real email sender | New service class | Medium effort |

---

## Verification Checklist

- [ ] `dotnet build` passes after each code change
- [ ] Login lockout: 6 failed attempts trigger lockout message
- [ ] Security headers visible in browser devtools Network tab
- [ ] Leaflet popup renders correctly for park names with `<`, `>`, `&` characters
- [ ] Logout with `?returnUrl=//evil.com` stays on site, does not redirect externally
- [ ] After Azure deployment: run [securityheaders.com](https://securityheaders.com) scan
- [ ] Azure Key Vault references resolve correctly in App Service
