## 1. Application changes

- [x] 1.1 Add `UseForwardedHeaders` (XForwardedFor | XForwardedProto, cleared KnownNetworks/KnownProxies, ForwardLimit = 1) as the first middleware in `Program.cs`, outside Development only
- [x] 1.2 Add an unauthenticated `GET /healthz` endpoint that returns 200 without touching the database
- [x] 1.3 Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, make `WishlistDbContext` implement `IDataProtectionKeyContext`, and register `AddDataProtection().PersistKeysToDbContext<WishlistDbContext>().SetApplicationName("wishlist")` in `AddInfrastructure`
- [x] 1.4 Add the EF Core migration `AddDataProtectionKeys` and apply it locally against docker-compose Postgres
- [x] 1.5 Remove the `BuildSpa` MSBuild target from `Wishlist.API.csproj`
- [x] 1.6 Add tests: forwarded `X-Forwarded-Proto: https` satisfies `[RequireHttps]`; `X-Forwarded-For` drives the per-IP reset limit; `/healthz` returns 200
- [x] 1.7 Verify local dev loop still works (`dotnet run` + `npm run dev`, login, refresh, password reset via smtp4dev)

## 2. Container image

- [x] 2.1 Add `.dockerignore` (bin/obj, node_modules, dist, .git, openspec)
- [x] 2.2 Add a multi-stage `Dockerfile`: Node build of `src/Wishlist.Web` → `dotnet publish` of `Wishlist.API` with SPA `dist/` copied into `wwwroot/` → `aspnet:9.0` runtime, non-root `app` user, `ASPNETCORE_HTTP_PORTS=8080`
- [x] 2.3 Build and run the image locally against docker-compose Postgres with Production-like env vars; verify SPA loads, deep-link fallback works, and `/healthz` returns 200

## 3. Infrastructure (Bicep)

- [x] 3.1 `infra/main.bicep` + `infra/main.parameters.json`: parameters for location (swedencentral), names, image, custom domain/public base URL, minReplicas (default 0), budget recipient, and `@secure()` secrets (Postgres admin password, JWT key, SMTP user/password, GHCR pull token)
- [x] 3.2 Log Analytics workspace module (PerGB2018, 30-day retention, 0.1 GB daily cap)
- [x] 3.3 PostgreSQL Flexible Server module: v17, Standard_B1ms Burstable, 32 GiB, 7-day backups, `require_secure_transport=on`, `AllowAzureServices` firewall rule, `wishlist` database
- [x] 3.4 Communication Services module: Email Communication Service + Azure-managed domain + linked Communication Service; output the sender address
- [x] 3.5 Container Apps module: Consumption environment wired to Log Analytics; `ca-wishlist` with external ingress on 8080, `allowInsecure: false`, 0.25 vCPU / 0.5 GiB, scale 0–1 (HTTP rule), liveness/readiness probes on `/healthz`, GHCR registry credential, secrets + env vars per design D7
- [x] 3.6 Budget module: $25/month on the resource group, alerts at 80% and 100% actual spend
- [ ] 3.7 `az bicep build` and `az deployment group what-if` succeed against an empty resource group

## 4. CI/CD (GitHub Actions)

- [x] 4.1 `.github/workflows/infra.yml`: `workflow_dispatch`, `environment: production`, OIDC `azure/login`, `az deployment group create` passing secrets from the environment
- [x] 4.2 `.github/workflows/deploy.yml` build job: `dotnet test`, SPA lint + build, docker build + push `ghcr.io/<owner>/wishlist:sha-<sha7>` (and `latest`), build a self-contained linux-x64 EF migrations bundle as an artifact
- [x] 4.3 `deploy.yml` deploy job (`environment: production`): OIDC login, add a temporary Postgres firewall rule for the runner IP, run the migrations bundle, remove the rule in an `always()` step, `az containerapp update --image`, smoke-check `GET /healthz` on the public URL
- [ ] 4.4 Confirm the workflow fails without updating the app when the migration step fails

## 5. Bootstrap docs and first deploy

- [x] 5.1 `infra/README.md`: one-time setup (resource group; Entra app + federated credential for `environment:production` + Contributor on the RG; ACS SMTP Entra app + role + SMTP username format; GHCR read token; GitHub `production` environment with required reviewer, variables and secrets), secret expiry dates, rollback and teardown
- [ ] 5.2 Run the bootstrap and `infra.yml`; confirm all resources exist and the monthly cost estimate in Azure Cost Management matches ~$15–20
- [ ] 5.3 Merge to `main`, approve the deploy, and verify end-to-end on the `azurecontainerapps.io` FQDN: register, login, reload keeps session, password reset email arrives via ACS
- [ ] 5.4 Verify scale to zero after idle and that a reset link issued before the scale-down still works after it
- [ ] 5.5 Bind the custom domain (CUSTOM_DOMAIN, then CUSTOM_DOMAIN_CERT_READY=true via infra.yml), update `App__PublicBaseUrl`, re-run `infra.yml`, re-verify login and reset links
- [x] 5.6 Update the root `README.md` with a short "Deployment" section pointing at `infra/README.md`
