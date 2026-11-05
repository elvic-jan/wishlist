## Context

Production is one Bicep template (`infra/main.bicep`) in resource group `rg-wishlist-prod`, deployed by `.github/workflows/deploy.yml` (build → `production` environment with approval) and `.github/workflows/infra.yml`. Resource names come from `baseName` plus a per-resource-group suffix, so a second copy needs no template changes for naming. The workflows, however, hard-code `ca-wishlist`, `caj-wishlist-reservations`, `Database=wishlist`, the `production` environment and the `deploy-production` concurrency group.

Relevant behaviour today:
- Passkeys belong to the host of `App:PublicBaseUrl` without `www.` (`PasskeyConfigOptions`). Without a custom domain, Bicep sets `App:PublicBaseUrl` to the app's `azurecontainerapps.io` address.
- `SmtpEmailSender` sends to any recipient.
- The repository is public. Fork pull requests run `ci.yml` on `pull_request` (read-only token, no secrets; first-time contributors need approval). Only the owner can push branches or start workflows by hand.
- Migrations run as the database admin; the app connects as `wishlist_app` (`infra/sql/app-role.sql`), whose password the deploy sets.

## Goals / Non-Goals

**Goals:**
- Try every `main` build, and any pull request on demand, on hosted infrastructure before production.
- No path from staging to production: separate credentials, database, Azure identity and passkey domain.
- Staging can never email real people by accident.
- Close to €0 per month.

**Non-Goals:**
- A custom domain for staging (can be added later with the existing two-run domain flow).
- Performance testing (the database is in another cloud and both app and database sleep).
- A copy of production data, ever.
- Moving the opt-in end-to-end test database to staging (later).
- One staging copy per pull request.

## Decisions

### 1. Address: the free `azurecontainerapps.io` host
Staging leaves `CUSTOM_DOMAIN` empty. A subdomain of `kadoozle.app` was rejected: WebAuthn lets a page on a subdomain ask for passkeys of its parent domain, so unapproved code on `staging.kadoozle.app` could request `kadoozle.app` sign-ins. The Azure host is fully separate and free. It changes if the Container Apps environment is ever recreated; with test data only that is acceptable.

### 2. Database: free external PostgreSQL (Neon assumed), EU region
Alternatives:
- *Separate database on the production server* — rejected. Logins are server-wide, so staging's `ALTER ROLE wishlist_app PASSWORD` would change production's password; staging would need the production admin password and Azure rights on the production resource group.
- *Own Azure Flexible Server* — fully isolated and identical to production but about €15/month.

Chosen for cost and isolation. Bicep gets `externalPostgresHost` (default empty = deploy the Azure server as today); when set, the Postgres module is skipped and the app's connection string is built from that host with database `wishlist`, login `wishlist_app` and `SSL Mode=VerifyFull`, exactly as for Azure. The host is the provider's **direct** endpoint, not its pooler (transaction pooling breaks Npgsql prepared statements). The staging database's owner login takes the admin role for migrations; `app-role.sql` runs unchanged and creates `wishlist_app` on that separate server, so login names stay the same in both environments.

The GitHub `staging` environment uses production's setting names (`POSTGRES_ADMIN_LOGIN`, `POSTGRES_ADMIN_PASSWORD`, `POSTGRES_APP_PASSWORD`, `JWT_KEY`, …) plus `POSTGRES_HOST`, `BASE_NAME=wishlist-stg`, `BUDGET_AMOUNT`, `APP_ENVIRONMENT_LABEL` and the secret `EMAIL_ALLOWLIST`. Email addresses (`EMAIL_ALLOWLIST`, `BUDGET_CONTACT_EMAIL`) are secrets in both environments: the repository is public, and a variable's value is printed in workflow logs. The base name is short because `caj-<base>-reservations` must fit Azure's 32-character limit.

Trade-off: the Azure-only database steps (server lookup, firewall open/close) don't run on staging, so a bug there still shows up first in production. Migrations, which fail more often, are exercised.

### 3. Workflow shape
```
push to main ─▶ build ─▶ deploy-staging ─▶ (smoke ✓) ─▶ deploy-production (approval)
manual run on a branch ─▶ build ─▶ deploy-staging          (stops)
```
- The deploy steps move into one reusable workflow (`deploy-environment.yml`, `workflow_call`) taking the environment name; environment variables supply the resource group, app and job names, database name, and whether Azure Postgres steps run. `deploy.yml` calls it twice.
- `deploy-production` has `needs: deploy-staging` and runs only on `main`. The gate is this run's build passing on staging; later deploys to staging (e.g. a pull request) don't affect it.
- Concurrency: `deploy-staging` and `deploy-production` groups, set on the jobs. A staging deploy never cancels a production deploy waiting for approval. `infra.yml` uses the group of the environment it targets.
- Pull request deploys: `workflow_dispatch` on the branch. No `pull_request_target`, `issue_comment` or label triggers, which would run with secrets on outsiders' input. The `staging` GitHub environment allows all branches (only the owner can create them); `production` keeps `main` only.
- Versions: a manual run on a branch other than `main` gets the version `X.Y.Z-pr.<sha7>`; only the production job tags releases and creates GitHub Releases. The smoke check compares `/healthz`'s version as today.

### 4. Azure identity
A separate Entra app registration for staging with a federated credential for `environment:staging` and Contributor only on the staging resource group. Production's identity is unchanged. A staging run therefore cannot change production resources even if a workflow file on a branch is altered.

### 5. Database reset only when migrations differ
The build writes the IDs of the migrations it contains (the file names in `src/Wishlist.Infrastructure/Migrations`) to `migrations.txt` next to the bundle. Before migrating, a deploy that allows resets (staging only) reads `__EFMigrationsHistory`. If the database has any applied migration that the build doesn't contain, it wipes the database with plain SQL (`DROP SCHEMA public CASCADE; CREATE SCHEMA public; GRANT USAGE ON SCHEMA public TO PUBLIC;`) and then migrates as usual. Otherwise it only migrates (normal case: new migrations on top). The reset step also refuses to run for the `production` environment.

Alternatives: restoring a seeded Neon branch through Neon's API (needs an API key and branch IDs, and ties staging to Neon); reset on every deploy (test accounts never survive a deploy); a database branch per pull request (the app's connection string changes per deploy).

### 6. Seeding
When the web app starts with an environment label, it creates its test data if the first test user doesn't exist yet: four users, two circles, wishes and a reservation. So after a reset the next start fills the database again, and an existing database is left alone. Job runs (`--job …`) never seed.

The users' addresses are `+tag` variants of the first exact address on the email allow-list (`you@example.com` → `you+anna@example.com`, …), so their mail reaches that inbox. Without such an address, seeding is skipped with a warning. The users have no password: sign in through "Forgot password", whose email arrives through the allow-list, then set a password or passkey. No seed secret, nothing printed.

### 7. Email allow-list
A decorator `AllowListEmailSender : IEmailSender` around `SmtpEmailSender`, registered only when `App:EnvironmentLabel` is set. Production has no label, so its code path is unchanged.
- `Smtp:AllowedRecipients`: comma-separated entries; `name@domain` (exact) or `*@domain` (whole domain). A listed `name@domain` also matches `name+anything@domain`. Comparison ignores case.
- Label set and list empty or missing → nothing is sent (fail closed).
- Allowed mail gets `[TEST] ` before the subject.
- Dropped mail is logged at Information with the subject and a masked recipient: first character of the local part kept, rest of it `*`, the `+tag` part shown in full, the domain's first character kept and the rest of the name masked with the TLD kept: `you+alice@gmail.com` → `y**+alice@g****.com`. Never the body or links.
- Staging has no `EMAIL_DOMAIN`, so it sends from the Azure-managed `azurecomm.net` address of its own ACS resource.

### 8. TEST marker
`App:EnvironmentLabel` (e.g. `TEST`) is added to `/healthz`'s answer (`environment`). The service worker caches the page itself, so the label can't be written into the HTML; `/healthz` is never cached by it and needs no database. The SPA fetches it at start, shows a non-dismissible banner at the top of every page, and remembers the label in `localStorage` so the banner is there at once on the next visit, even while the app wakes up. Every response gets `X-Robots-Tag: noindex`. Production has no label: no banner, no header, and `/healthz` has no `environment` value.

### 9. Reservation job
Created by the same template on staging with the same monthly schedule; its mail goes through the allow-list. Can be started by hand (`az containerapp job start`) to test.

### 10. Budget
The template's existing budget module, on the staging resource group, with a small amount (e.g. 5) and auto-stop on.

## Risks / Trade-offs

- [Free tier limits or terms change] → staging stops until the next month or moves; only seeded test data is at stake. The Bicep switch also allows an own Azure server later.
- [Provider's owner login lacks a right `app-role.sql` needs (CREATE ROLE, ALTER DEFAULT PRIVILEGES)] → check in the first task; fall back to creating `wishlist_app` in the provider's console and only granting in the script.
- [Staging database is reachable from the internet with a password only] → test data only; strong generated password; the app login has row rights only.
- [Public workflow logs show staging's address and resource names] → identifiers, not credentials; secrets stay masked.
- [Someone edits the workflow on a branch to target production] → the branch's run gets only the staging identity and secrets; `production` accepts `main` only and needs approval.
- [Allow-list misconfigured] → fail closed: no mail. Unit tests cover matching and masking.
- [Cold starts: app and database both sleep] → first request takes several seconds; fine for testing.

## Migration Plan

1. Create the external database (EU) and the owner credentials. (Done.)
2. Create the staging resource group, Entra app and federated credential, the GitHub `staging` environment with variables and secrets. (Done, apart from the variables this change introduces.)
3. Merge the change; run `infra` for staging; finish the staging SMTP and budget-stop roles; the next push to `main` deploys to staging (which seeds itself), then waits for production approval as before.

Rollback: remove the `deploy-staging` job and the `needs` from `deploy-production`; production is otherwise unchanged.

## Open Questions

- The budget amount for staging (5 suggested).
