## 1. Prerequisites (owner, outside the repo)

- [x] 1.1 Create a free external PostgreSQL project (Neon) in an EU region, PostgreSQL 17, with database `wishlist`; note the direct (not pooled) host
- [x] 1.2 Check that the database owner can run `infra/sql/app-role.sql` (CREATE ROLE, ALTER DEFAULT PRIVILEGES)
- [x] 1.3 Create resource group `rg-wishlist-staging`, an Entra app with a federated credential for `environment:staging` and Contributor on that group only, and a staging SMTP Entra app
- [x] 1.4 Create the GitHub `staging` environment (all branches, no approval) with Azure IDs, resource group, budget email, Postgres host and login, and secrets (Postgres admin and app passwords, JWT key, GHCR token, SMTP credentials)

## 2. Infrastructure

- [x] 2.1 `main.bicep`: `externalPostgresHost`; skip the Postgres module when set and build the connection from that host
- [x] 2.2 `main.bicep` / `containerapp.bicep`: `environmentLabel` and `emailAllowList` passed as `App__EnvironmentLabel` and `Smtp__AllowedRecipients` (only when set)
- [x] 2.3 `infra.yml`: choose the environment (input, default production); base name, budget amount, Postgres host, label and allow-list from environment variables; app name from the base name; concurrency group per environment
- [x] 2.4 `infra/README.md`: staging setup (variables, roles with their own names, first run), the external database, and the rule that no deploy uses `pull_request_target`, comment or label triggers

## 3. Deploy workflow

- [x] 3.1 Move the deploy steps into a reusable `deploy-environment.yml` taking the environment; names from `BASE_NAME`; Azure Postgres lookup and firewall steps only without `POSTGRES_HOST`
- [x] 3.2 `deploy.yml`: `deploy-staging` (automatic, concurrency `deploy-staging`) then `deploy-production` (`needs: deploy-staging`, `main` only, concurrency `deploy-production`)
- [x] 3.3 Manual runs on a branch other than `main`: pre-release version `X.Y.Z-pr.<sha7>`, staging only; tagging, GitHub Release and image version tag stay in the production job
- [x] 3.4 Build writes `migrations.txt`; staging deploy empties the database when it has a migration the build lacks (never for production), then migrates

## 4. API

- [x] 4.1 `App:EnvironmentLabel` option; `environment` in `/healthz` when set
- [x] 4.2 `AllowListEmailSender` decorator: matching (exact, `+tag`, `*@domain`, case-insensitive), `[TEST] ` subject prefix, fail closed on an empty list, drop log with subject and masked recipient (`you+alice@gmail.com` → `y**+alice@g****.com`); registered only when a label is set
- [x] 4.3 `X-Robots-Tag: noindex` on every response when a label is set
- [x] 4.4 Seed test data at web start when a label is set and the test users don't exist: `+tag` users of the first exact allow-list address, no password, circles, wishes, a reservation; skipped in job runs and without such an address
- [x] 4.5 Unit tests: matching, masking, fail-closed, prefix; noindex and `/healthz` label only with a label; seeding addresses and skip rules

## 5. SPA

- [x] 5.1 Non-dismissible label banner at the top of every page from `/healthz`, remembered in `localStorage`; nothing in production
- [x] 5.2 Lint and build

## 6. Bring staging up (before merging: after the merge, production waits for staging)

- [x] 6.1 Set variables `BASE_NAME`, `BUDGET_AMOUNT`, `APP_ENVIRONMENT_LABEL` and secrets `EMAIL_ALLOWLIST`, `BUDGET_CONTACT_EMAIL` on `staging` (and `BUDGET_CONTACT_EMAIL` as a secret on `production`; its variable goes after the merge); run `infra` for staging from this branch; finish SMTP and budget-stop roles; run `deploy` on this branch
- [x] 6.2 Check on a phone: TEST banner, test user sign-in through "Forgot password", sign up with a passkey, an allow-listed email arrives with `[TEST]`, an unlisted one is dropped and logged masked, PWA update notice
- [ ] 6.3 Deploy a branch with an extra migration, then `main`: database is emptied, migrated and re-seeded, and `main` works
- [ ] 6.4 Start the staging reservation job by hand; it finishes and mails only allow-listed recipients
- [ ] 6.5 Confirm production deploy waited for staging and still needs approval

## 7. Release

- [x] 7.1 Release note: none (nothing changes for people using Kadoozle); confirmed with the owner
