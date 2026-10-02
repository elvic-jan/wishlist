## Context

The app runs today only as a local dev loop: Vite SPA on `https://localhost:5173` proxying `/api` to the ASP.NET Core 9 API on `https://localhost:7277`, Postgres 17 in docker-compose, and smtp4dev for mail. Nothing is deployed.

An earlier plan (`add-azure-infrastructure`, never merged) designed a private-by-default, two-environment Azure estate. It was correct but far too expensive for a family app (dedicated workload profiles alone are about $150/month per environment). This design aims at the opposite end: the cheapest setup that is still secure, reproducible (Bicep + CI) and pleasant to operate.

Facts about the current code that shape the design:

- `Program.cs` already calls `UseStaticFiles()` and `MapFallbackToFile("index.html")`, so the API can serve the SPA bundle from `wwwroot` with no new code.
- The SPA calls `/api` relative to its own origin (`client.ts` `baseURL: '/api'`), so serving both from one origin needs no SPA change.
- `AuthController` is `[RequireHttps]` and the refresh cookie is `Secure; HttpOnly; SameSite=Strict`.
- `ForgotPasswordRateLimiter` is in-process and keyed partly on `HttpContext.Connection.RemoteIpAddress`.
- Identity reset tokens are protected by ASP.NET Core Data Protection, whose keys are currently stored in the default ephemeral location.
- `ValidateRequiredOptions` already fails startup in non-Development environments if `Smtp:Host` or an HTTPS `App:PublicBaseUrl` is missing.

## Goals / Non-Goals

**Goals:**
- Run the app in Azure at about $15–20/month total, with the database as the only meaningful fixed cost.
- One public HTTPS origin on a custom domain, with a free managed certificate.
- Reproducible infrastructure (Bicep) and hands-off deploys from `main` (GitHub Actions), with a manual approval before production changes.
- No long-lived Azure credentials in GitHub.
- Keep local development exactly as it is.

**Non-Goals:**
- Test/staging environments, network isolation, WAF/CDN, multi-region, horizontal scale-out, live config refresh. Each can be added later without redoing this design.

## Decisions

### D1. Compute: Azure Container Apps, Consumption plan, scale 0–1

One Consumption-only Container Apps environment (no VNet, no workload profile) with one app, `ca-wishlist`. It has external ingress on target port 8080, `allowInsecure: false` (HTTP is redirected to HTTPS at the edge), `minReplicas: 0`, `maxReplicas: 1`, and 0.25 vCPU / 0.5 GiB.

- The monthly free grant (180,000 vCPU-seconds, 360,000 GiB-seconds, 2M requests) covers a family app's traffic, so expected compute cost is $0.
- `maxReplicas: 1` keeps the in-process forgot-password rate limiter correct.
- `minReplicas` is a Bicep parameter. Setting it to 1 removes cold starts for roughly $10–15/month.

*Alternatives:* App Service F1 (free, but 60 CPU-minutes/day and no custom-domain TLS); App Service B1 (about $13/month, always on, no scale to zero); Static Web Apps + Functions (would mean rewriting the API). Container Apps Consumption is the only option that is effectively free *and* runs the existing container unchanged.

### D2. One container: API serves the SPA

A multi-stage `Dockerfile` at the repo root:
1. `node:22-alpine`: `npm ci && npm run build` in `src/Wishlist.Web`.
2. `mcr.microsoft.com/dotnet/sdk:9.0`: `dotnet publish src/Wishlist.API -c Release`, with the SPA `dist/` copied into the publish output's `wwwroot/`.
3. `mcr.microsoft.com/dotnet/aspnet:9.0`: runs as the built-in non-root `app` user with `ASPNETCORE_HTTP_PORTS=8080`.

The `BuildSpa` MSBuild target in `Wishlist.API.csproj` is removed. It ran `npm` inside every Release build, which is awkward in a .NET-only build stage, and the Dockerfile now owns that step.

*Alternative:* separate SPA (nginx) and API containers, as in the old plan. That gives two apps, two images and a proxy hop for no benefit at this scale.

### D3. TLS at the edge + forwarded headers

TLS terminates at the Container Apps ingress, so the container receives plain HTTP. `Program.cs` adds `UseForwardedHeaders` (for `XForwardedFor | XForwardedProto`) as the first middleware, but only outside Development. `KnownNetworks`/`KnownProxies` are cleared because ingress addresses are not fixed. This is safe because the container has no other route in: the only path to port 8080 is through the ingress. `ForwardLimit = 1`.

Effects: `Request.IsHttps` is true for real HTTPS traffic, so `[RequireHttps]` and `UseHttpsRedirection` behave correctly. `RemoteIpAddress` becomes the real client IP, so the per-IP reset limit works.

### D4. Data Protection keys persisted in Postgres

Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, make `WishlistDbContext` implement `IDataProtectionKeyContext` (`DbSet<DataProtectionKey> DataProtectionKeys`), and call `AddDataProtection().PersistKeysToDbContext<WishlistDbContext>().SetApplicationName("wishlist")`. This applies in all environments, so local dev exercises the same path.

*Why:* with scale to zero the container restarts many times a day. Ephemeral keys would make every outstanding 30-minute reset link fail after an idle period. Storing keys in the database costs nothing. Encrypting keys at rest (Key Vault) is skipped: the keys protect only short-lived reset tokens, and the database is already the system of record for password hashes.

### D5. Registry: GitHub Container Registry

Images are pushed to `ghcr.io/<owner>/wishlist` with an immutable tag `sha-<git sha7>` (plus a floating `latest` for humans). The CI job pushes using the workflow's built-in `GITHUB_TOKEN` (`packages: write`). The container app pulls with a GitHub token that has `read:packages`, stored as a Container App registry secret.

*Alternative:* ACR Basic (about $5/month) with managed-identity pull. That's cleaner auth, but a fixed cost for no functional gain.

### D6. Database: PostgreSQL Flexible Server, Burstable B1ms

PostgreSQL 17 (matching local), B1ms (1 vCore, 2 GiB), 32 GiB storage, 7-day backup retention, no HA, no geo-redundant backup. Settings: public access, `require_secure_transport = on`, and one firewall rule `AllowAzureServices` (0.0.0.0–0.0.0.0). There is one application database, `wishlist`. The app connects as the server admin for now (a dedicated least-privilege role is a follow-up).

Migrations run from CI, not at app startup. The deploy job adds a temporary firewall rule for the runner's public IP, runs an EF Core migrations bundle (`dotnet ef migrations bundle`) against the server, and removes the rule in an `always()` step. This keeps startup fast (cold starts matter here) and keeps schema changes behind the same approval gate as the code.

A design-time `WishlistDbContextFactory` (`IDesignTimeDbContextFactory`) lets the bundle build the context without booting the API host. Otherwise the startup validation (which requires `Smtp:Host` etc.) would abort the bundle on the runner.

*Alternatives:* migrate at startup (simpler, but slower cold starts, and a bad migration crash-loops the app); a Container Apps Job that runs migrations from inside Azure (avoids the temporary firewall rule, but adds a second resource and log-scraping to see results).

### D7. Configuration: environment variables + Container App secrets

Standard ASP.NET Core `__` env-var mapping, no new config code:

| Variable | Source |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | plain |
| `App__PublicBaseUrl=https://<custom domain>` | plain |
| `Smtp__Host=smtp.azurecomm.net`, `Smtp__Port=587`, `Smtp__UseStartTls=true`, `Smtp__FromAddress`, `Smtp__FromName` | plain |
| `ConnectionStrings__DefaultConnection` | secret |
| `Jwt__Key` | secret |
| `Smtp__User`, `Smtp__Password` | secret |

Secret values live in the GitHub `production` environment's secrets and are passed to Bicep as `@secure()` parameters by the infra workflow. Rotating a secret means updating the GitHub secret and re-running the infra workflow, which creates a new revision.

*Alternative:* Key Vault references from Container Apps (about $0.03/10k operations, practically free) with a managed identity. That's a reasonable upgrade later; skipped now to avoid an extra resource and RBAC wiring.

### D8. Email: Azure Communication Services Email over SMTP

An Email Communication Service with an **Azure-managed domain** (`DoNotReply@<guid>.azurecomm.net`, free, no DNS), linked to a Communication Service. ACS SMTP authenticates with an Entra app registration that has the "Communication and Email Service Owner" role (or a custom send role). The SMTP username is `<acs-resource>.<app-id>.<tenant-id>` and the password is the app's client secret. MailKit with STARTTLS on 587 works unchanged. Price: about $0.00025 per email.

A custom sender domain (e.g. `noreply@<your domain>`) is an optional later step that needs SPF/DKIM records.

### D9. CI/CD: two GitHub Actions workflows, OIDC to Azure

- **`infra.yml`** (manual `workflow_dispatch`, environment `production`): `az deployment group create` with `infra/main.bicep`. It's run rarely (first setup, secret rotation, infra changes).
- **`deploy.yml`** (push to `main`):
  1. `build` job (no approval): restore, `dotnet test`, SPA `npm ci && npm run build` + lint, `docker build`, push `ghcr.io/...:sha-xxxxxxx`, build the EF migrations bundle as an artifact.
  2. `deploy` job (`environment: production`, which requires a manual approval in GitHub): `azure/login` via OIDC; temporary firewall rule → run migrations bundle → remove rule; `az containerapp update --image ...:sha-xxxxxxx`; smoke-check `GET https://<domain>/healthz`.

OIDC: one Entra app registration with a federated credential for `repo:<owner>/wishlist:environment:production`, granted `Contributor` on `rg-wishlist-prod` only. GitHub stores only `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` (none of them secret).

GitHub-hosted runners: free on public repos and 2,000 min/month on private repos, which is plenty.

### D10. Observability and cost guardrails

- A Log Analytics workspace (PerGB2018, 30-day retention, **0.1 GB daily cap**) attached to the Container Apps environment for console logs. `appsettings.Production.json` lowers `Microsoft.EntityFrameworkCore` logging to Warning, because per-query SQL logs would quickly use up the cap.
- `GET /healthz` returns 200 without touching the database, so probes neither wake nor depend on Postgres. It's used for liveness/readiness probes and the CI smoke check.
- An **Azure budget** on the resource group: $25/month, emailing alerts at 80% and 100% of actual spend. It's declared in Bicep and free.

### D11. Custom domain

A managed certificate can only be issued once the hostname is already on the app and DNS validates, so Bicep cannot do it in one pass. The Bicep handles it in two phases driven by parameters, so re-running `infra.yml` never drops the binding (which a manual `az containerapp hostname bind` would risk): with `customDomainName` set, the hostname is added with `bindingType: Disabled`; once the `CNAME` and `asuid` `TXT` records exist, `customDomainCertificateReady=true` creates the managed certificate, binds it with SNI, and switches `App__PublicBaseUrl` to the custom domain. Until then the app is usable on its `*.azurecontainerapps.io` FQDN, with `App__PublicBaseUrl` pointing there.

## Risks / Trade-offs

- **Cold start after idle (about 5–15 s)** → acceptable for a family app; set `minReplicas = 1` if it annoys. The SPA shows its normal loading state.
- **`AllowAzureServices` firewall rule admits any Azure-hosted IP, not just ours** → SSL is required, a long random admin password is used, and Postgres has no other exposure. A VNet or private endpoint costs more and is the upgrade path.
- **The GHCR pull token and the ACS SMTP client secret both expire** → record their expiry dates in `infra/README.md`. The budget/health alerting will surface a broken deploy; failed mail sends are already logged. (Making the GHCR package public would remove the pull token, since the image contains no secrets. See Open Questions.)
- **The single replica is a single point of failure** → Container Apps restarts unhealthy replicas automatically. Downtime during a revision swap is a few seconds.
- **Losing the Azure free grant** (e.g. if other Container Apps in the same subscription use it up) → the budget alert catches it. Even at full price, 0.25 vCPU for a few hours a day is under $5/month.
- **B1ms burst credits**: sustained heavy load would throttle → not realistic at this scale; vertical scaling is a parameter change.
- **Migrations are forward-only**: a rollback of the image does not roll back the schema → keep migrations additive (expand/contract). Rollback = re-run `deploy` for an older `sha-` tag.

## Migration Plan

1. One-time bootstrap (manual, documented in `infra/README.md`): create the resource group, the Entra app with federated credential + role assignment, the ACS SMTP app registration, the GitHub `production` environment with required reviewer and secrets, and the GHCR pull token.
2. Run `infra.yml` → all Azure resources exist; the container app runs the placeholder image.
3. Merge to `main` → `deploy.yml` builds, migrates the empty database to the latest schema, and deploys the real image.
4. Bind the custom domain, update `App__PublicBaseUrl`, re-run `infra.yml`.

Rollback: re-deploy a previous `sha-` image tag. Teardown: delete `rg-wishlist-prod` (Postgres backups go with it; take a `pg_dump` first).

## Open Questions

- **Custom domain name**: needed for `App__PublicBaseUrl` and the DNS steps. The `azurecontainerapps.io` FQDN is used until it's known.
- **GHCR package visibility**: private with a pull token (default) or public (no token to rotate)?
- **Budget alert recipient**: defaults to the subscription owner's email, passed as a Bicep parameter.
