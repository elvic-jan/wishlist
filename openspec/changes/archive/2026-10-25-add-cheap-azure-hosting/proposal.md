## Why

The wishlist app is ready to host, but the earlier `add-azure-infrastructure` plan (two VNet-integrated Container Apps environments on dedicated workload profiles, ACR Premium, App Configuration, per-env Key Vaults, private endpoints, a self-hosted runner VM) would cost several hundred dollars a month for a family-sized app. That plan is abandoned. This change gets the app onto Azure for roughly **$15–20/month**, almost all of which is the Postgres server. It favours managed, free-tier or pay-per-use services and keeps a clear path to harden later if usage grows.

## What Changes

### Infrastructure (new `infra/` Bicep + GitHub Actions workflows)

- **One environment (production), one resource group** `rg-wishlist-prod` in **Sweden Central**.
- **Azure Container Apps on the Consumption plan**: a single `wishlist` container app with external HTTPS ingress, `minReplicas = 0` (scale to zero), `maxReplicas = 1`. No VNet and no workload profile.
- **Single container image**: the ASP.NET Core API serves the built Vite SPA from `wwwroot` (the app already calls `UseStaticFiles()` and `MapFallbackToFile("index.html")`). The browser sees one origin, so the `Secure` + `SameSite=Strict` refresh cookie works unchanged.
- **Custom domain with a free managed certificate** on the container app.
- **GitHub Container Registry (`ghcr.io`)** for images instead of ACR. The container app pulls with a read-only token stored as a Container App secret.
- **Azure Database for PostgreSQL Flexible Server, Burstable B1ms**, 32 GB storage, 7-day backups, public network access limited to Azure services, SSL required.
- **Azure Communication Services Email** with a free Azure-managed sender domain, used through its SMTP endpoint so `SmtpEmailSender` (MailKit) is unchanged.
- **Configuration and secrets as Container App secrets and environment variables**: no App Configuration, no Key Vault.
- **Log Analytics workspace** (pay-per-GB, low daily cap) for container logs.
- **GitHub Actions on GitHub-hosted runners**: a workflow that tests, builds and pushes the image to GHCR on `main`, applies EF Core migrations, then deploys the new revision. It authenticates to Azure via OIDC federated credentials (no stored Azure secrets). A manual-approval GitHub Environment (`production`) gates the deploy job.

### Application changes

- **Forwarded headers**: the app trusts `X-Forwarded-Proto` / `X-Forwarded-For` from the Container Apps ingress. Without this, `[RequireHttps]` would reject every auth call (TLS terminates at the ingress, so the container sees plain HTTP), and the per-IP forgot-password rate limit would put every user in one bucket.
- **Persist ASP.NET Core Data Protection keys in Postgres**: scale-to-zero restarts the container often, and with ephemeral keys every restart would invalidate outstanding password-reset links.
- **Dockerfile** (multi-stage: Node build of the SPA, `dotnet publish`, `aspnet` runtime image listening on HTTP 8080 as a non-root user). The `BuildSpa` MSBuild target is removed; the Dockerfile builds the SPA explicitly.
- **Health endpoint** `GET /healthz` for the ingress probes.

### Out of scope

- A test/staging environment (it can be added later as a second container app in the same Container Apps environment at near-zero cost).
- Network isolation (VNet, private endpoints), WAF/CDN, multi-region DR.
- Live configuration refresh. Changing a setting creates a new revision (about 30 s).
- Scaling past one replica (needs a shared store for the forgot-password rate limiter).

## Capabilities

### New Capabilities

- `hosting`: how the app is packaged, deployed and configured in Azure: single-container image, scale-to-zero compute capped at one replica, configuration via environment variables and platform secrets, migrations at deploy time, gated production deploys, and the cost ceiling that motivates these choices.

### Modified Capabilities

- `user-auth`: the "Authentication traffic served only over HTTPS" requirement currently says the API never binds an HTTP listener in any environment. In Azure, TLS terminates at the platform ingress (which refuses plain HTTP), and the container listens on HTTP inside the platform. The requirement is restated so HTTPS is enforced at the public edge, and the app derives the request scheme and client IP from the trusted ingress's forwarded headers.

## Impact

- **New files**: `Dockerfile`, `.dockerignore`, `infra/main.bicep` (and modules), `infra/main.parameters.json`, `.github/workflows/deploy.yml`, `infra/README.md` (one-time bootstrap: OIDC app registration, ACS domain, DNS records).
- **Changed code**:
  - `src/Wishlist.API/Program.cs`: forwarded headers, health endpoint, Data Protection key persistence.
  - `src/Wishlist.API/Wishlist.API.csproj`: remove the `BuildSpa` target.
  - `src/Wishlist.Infrastructure`: `DataProtectionKeys` table on `WishlistDbContext` plus a new migration; new package `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`.
- **Local development is unchanged** (Kestrel HTTPS, Vite HTTPS, docker-compose Postgres).
- **Cost**: about $13–16 for Postgres B1ms + storage, $0 for Container Apps (inside the monthly free grant), $0 for GHCR, a few cents for ACS Email and Log Analytics.
