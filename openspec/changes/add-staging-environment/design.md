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

Chosen for cost and isolation. Bicep gets `deployPostgres` (default `true`) and `databaseConnectionString` (secure, used when `deployPostgres` is `false`). The staging connection string points at the provider's **direct** endpoint, not its pooler (transaction pooling breaks Npgsql prepared statements), with `SSL Mode=VerifyFull`. The staging database's owner login takes the admin role for migrations; `app-role.sql` runs unchanged and creates `wishlist_app` on that separate server, so login names stay the same in both environments.

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
Before migrating, the staging deploy compares the migrations in this build's bundle (`efbundle --list` or the migrations assembly) with `__EFMigrationsHistory`. If the database has any applied migration that the bundle doesn't contain, the deploy resets the database to the clean seeded state and then migrates. Otherwise it only migrates (normal case: new migrations on top).

Reset mechanism: restore the provider's `seed` branch onto the staging branch (Neon "reset from parent") via its API, with an API key as a staging secret. Fallback if the provider changes: drop and recreate the `public` schema, migrate, run `--job seed-test-data`.

Alternatives: reset on every deploy (simplest, but test accounts never survive a deploy); a database branch per pull request (cleanest, but the app's connection string changes per deploy).

### 6. Seeding
`--job seed-test-data` (same pattern as `owner-access`): creates a few test users with addresses on the allow-list (`+tag` variants of one inbox), two circles, wishes and a reservation. It refuses to run unless the environment label is set. Passwords come from a staging secret; nothing is printed. Run once to build the `seed` branch, and after any fallback reset.

### 7. Email allow-list
A decorator `AllowListEmailSender : IEmailSender` around `SmtpEmailSender`, registered only when `App:EnvironmentLabel` is set. Production has no label, so its code path is unchanged.
- `Smtp:AllowedRecipients`: comma-separated entries; `name@domain` (exact) or `*@domain` (whole domain). A listed `name@domain` also matches `name+anything@domain`. Comparison ignores case.
- Label set and list empty or missing → nothing is sent (fail closed).
- Allowed mail gets `[TEST] ` before the subject.
- Dropped mail is logged at Information with the subject and a masked recipient: first character of the local part kept, rest of it `*`, the `+tag` part shown in full, the domain's first character kept and the rest of the name masked with the TLD kept: `you+alice@gmail.com` → `y**+alice@g****.com`. Never the body or links.
- Staging has no `EMAIL_DOMAIN`, so it sends from the Azure-managed `azurecomm.net` address of its own ACS resource.

### 8. TEST marker
`App:EnvironmentLabel` (e.g. `TEST`) reaches the SPA with the existing app config the SPA already loads (or a small `/api/app-info` field). When set: a fixed, non-dismissible banner at the top of every page, and `X-Robots-Tag: noindex` on every response. Production renders nothing extra.

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

1. Create the external database (EU), a `seed` branch, and the owner credentials.
2. Create the staging resource group, Entra app and federated credential, the GitHub `staging` environment with variables and secrets.
3. Merge the change; run `infra` for staging; the next push to `main` deploys to staging, then waits for production approval as before.
4. Seed, then save the seeded state as the `seed` branch.

Rollback: remove the `deploy-staging` job and the `needs` from `deploy-production`; production is otherwise unchanged.

## Open Questions

- The exact way the SPA learns the label (existing config endpoint vs. a new field) — decide while implementing.
- The budget amount for staging (5 suggested).
