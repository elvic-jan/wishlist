## 1. Prerequisites (owner, outside the repo)

- [ ] 1.1 Create a free external PostgreSQL project (Neon assumed) in an EU region, PostgreSQL 17; note the direct (not pooled) connection string and an API key
- [ ] 1.2 Check that the database owner can run `infra/sql/app-role.sql` (CREATE ROLE, ALTER DEFAULT PRIVILEGES); if not, create `wishlist_app` in the provider's console and adjust the script to skip creation
- [ ] 1.3 Create resource group `rg-wishlist-staging`, an Entra app with a federated credential for `environment:staging` and Contributor on that group only
- [ ] 1.4 Create the GitHub `staging` environment (all branches, no approval) with its variables (resource group, Azure IDs, `APP_ENVIRONMENT_LABEL=TEST`, `EMAIL_ALLOWLIST`, `BUDGET_CONTACT_EMAIL`) and secrets (DB owner and app passwords or connection strings, JWT key, SMTP credentials, provider API key, seed password)

## 2. Infrastructure

- [ ] 2.1 `main.bicep`: `deployPostgres` switch and secure `databaseConnectionString`; skip the Postgres module when off
- [ ] 2.2 `main.bicep` / `containerapp.bicep`: `environmentLabel` and `emailAllowList` parameters passed as `App__EnvironmentLabel` and `Smtp__AllowedRecipients` (only when set)
- [ ] 2.3 Budget amount and auto-stop as parameters; staging uses a small amount
- [ ] 2.4 `infra.yml`: choose the environment (input), take app and job names from variables instead of `ca-wishlist`, concurrency group per environment
- [ ] 2.5 `infra/README.md`: staging setup, the external database, and the rule that no deploy uses `pull_request_target`, comment or label triggers

## 3. Deploy workflow

- [ ] 3.1 Move the deploy steps into a reusable `deploy-environment.yml` taking the environment; names and database settings from environment variables; Azure Postgres lookup and firewall steps only when the database is on Azure
- [ ] 3.2 `deploy.yml`: `deploy-staging` (automatic, concurrency `deploy-staging`) then `deploy-production` (`needs: deploy-staging`, `main` only, concurrency `deploy-production`)
- [ ] 3.3 Manual runs on a branch other than `main`: pre-release version `X.Y.Z-pr.<sha7>`, staging only; tagging, GitHub Release and image version tag stay in the production job
- [ ] 3.4 Staging: compare applied migrations with the bundle; when the database has one the bundle lacks, reset to the `seed` branch through the provider's API, then migrate
- [ ] 3.5 Check that a staging deploy started while a production deploy waits for approval doesn't cancel it

## 4. API

- [ ] 4.1 `App:EnvironmentLabel` option; expose it to the SPA
- [ ] 4.2 `AllowListEmailSender` decorator: matching (exact, `+tag`, `*@domain`, case-insensitive), `[TEST] ` subject prefix, fail closed on an empty list, drop log with subject and masked recipient (`you+alice@gmail.com` → `y**+alice@g****.com`); registered only when a label is set
- [ ] 4.3 `X-Robots-Tag: noindex` on every response when a label is set
- [ ] 4.4 `--job seed-test-data`: test users, circles, wishes, a reservation; refuses without a label; password from configuration; prints no secrets
- [ ] 4.5 Unit tests: matching, masking, fail-closed, prefix, no-label passthrough; seed job refusal; noindex header only with a label

## 5. SPA

- [ ] 5.1 Non-dismissible label banner at the top of every page when a label is set; nothing in production
- [ ] 5.2 Lint and build

## 6. Bring staging up

- [ ] 6.1 Run `infra` for staging; deploy `main`; seed; save the seeded state as the provider's `seed` branch
- [ ] 6.2 Check on a phone: TEST banner, sign up with a passkey, an allow-listed email arrives with `[TEST]`, an unlisted one is dropped and logged masked, PWA update notice
- [ ] 6.3 Deploy a branch with an extra migration, then `main`: database is reset and `main` works
- [ ] 6.4 Start the staging reservation job by hand; it finishes and mails only allow-listed recipients
- [ ] 6.5 Confirm production deploy waited for staging and still needs approval

## 7. Release

- [ ] 7.1 Release note: none expected (nothing changes for people using Kadoozle); confirm with the owner before opening the PR
