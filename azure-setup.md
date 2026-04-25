# Azure Deployment Setup Guide

This guide covers the one-time Azure infrastructure setup required to deploy ParkTracker.
The GitHub Actions CI/CD pipeline (`.github/workflows/azure-deploy.yml`) is already in place
and will automatically build and deploy on every push to `main`.

---

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in (`az login`)
- An Azure subscription
- Admin access to the GitHub repository

---

## Step 1 — Create Infrastructure (Azure CLI)

```bash
# Create a resource group
az group create --name ParkTrackerRG --location centralus

# Create SQL Server logical server
az sql server create \
  --resource-group ParkTrackerRG \
  --name parktracker-db \
  --location centralus \
  --admin-user sqladmin \
  --admin-password <STRONG_PASSWORD>

# Create the database (Basic tier is fine for a small app)
az sql db create \
  --resource-group ParkTrackerRG \
  --server parktracker-db \
  --name parktracker \
  --service-objective Basic

# Create App Service Plan (Linux, B1 tier)
az appservice plan create \
  --name ParkTrackerPlan \
  --resource-group ParkTrackerRG \
  --sku B1 \
  --is-linux

# Create Web App (.NET 10)
az webapp create \
  --name parktracker-app \
  --resource-group ParkTrackerRG \
  --plan ParkTrackerPlan \
  --runtime "DOTNETCORE:10.0"
```

---

## Step 2 — Allow App Service to Reach SQL Server

```bash
# Allow connections from Azure services (covers App Service outbound IPs)
az sql server firewall-rule create \
  --resource-group ParkTrackerRG \
  --server parktracker-db \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

---

## Step 3 — Configure Application Settings

In the Azure Portal:

1. Navigate to **parktracker-app** → **Configuration** → **Application settings**
2. Add the following entries (click **New application setting** for each):

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:parktracker-db.database.windows.net,1433;Initial Catalog=parktracker;User ID=sqladmin;Password=<STRONG_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |
| `AdminSettings__Email` | your admin email address |
| `AdminSettings__Password` | your admin account password |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

3. Click **Save** and confirm the restart

> The double-underscore `__` in names maps to nested keys in `appsettings.json`
> (e.g., `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`).

---

## Step 4 — Connect GitHub Actions to Azure

### 4a. Create a Service Principal

```bash
# Note the clientId, clientSecret, tenantId, and subscriptionId from the output
az ad sp create-for-rbac --name parktracker-sp --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/ParkTrackerRG \
  --json-auth
```

### 4b. Add a Federated Credential

This allows GitHub Actions to authenticate without a stored password.

```bash
az ad app federated-credential create \
  --id <CLIENT_ID_FROM_ABOVE> \
  --parameters '{
    "name": "parktracker-github",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:michaelfariborz/ParkTracker:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 4c. Add Secrets to GitHub

1. In your GitHub repository, go to **Settings** → **Secrets and variables** → **Actions**
2. Add the following three secrets:

| Secret name | Value |
|-------------|-------|
| `AZURE_CLIENT_ID` | `clientId` from Step 4a output |
| `AZURE_TENANT_ID` | `tenantId` from Step 4a output |
| `AZURE_SUBSCRIPTION_ID` | `subscriptionId` from Step 4a output |

---

## Step 5 — Deploy

Push to `main` to trigger the first deployment:

```bash
git push origin main
```

Monitor progress under the **Actions** tab in GitHub. On success, the app will be live at:

```
https://parktracker-app.azurewebsites.net
```

---

## Troubleshooting

- **App fails to start:** Portal → **parktracker-app** → **Log stream** — startup errors (migrations, seed data, missing config) will appear here.
- **Database connection refused:** Confirm the firewall rule in Step 2 was applied and the connection string values in Step 3 are correct. The SQL Server name in the connection string must match the logical server name (`parktracker-db.database.windows.net`).
- **GitHub Actions fails:** Check the Actions tab for build/publish errors. Ensure the `AZURE_WEBAPP_PUBLISH_PROFILE` secret is set and the publish profile hasn't expired (re-download if needed).

---

## Notes

- The app automatically applies EF Core migrations and seeds the national parks data on first startup — no manual migration step needed.
- **ARR Affinity** is enabled on App Service by default. Leave it enabled — Blazor Server requires sticky sessions to maintain SignalR connections.
- To rename the app from `parktracker-app`, update `AZURE_WEBAPP_NAME` in `.github/workflows/azure-deploy.yml` to match.
