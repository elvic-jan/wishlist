## Why

Changes reach users the first time they run on a real hosted setup: phones, passkeys, emails and PWA updates can only be checked locally or in production. A staging environment next to production (issue #137) lets every change on `main`, and any pull request on demand, be tried on hosting like production's before it is approved for production.

## What Changes

- A second copy of the Bicep template, in its own resource group, scaling to zero, on its free `*.azurecontainerapps.io` address (no custom domain for now). Passkeys there belong to that hostname, fully separate from `kadoozle.app`.
- The staging database is a free external PostgreSQL (EU region), not an Azure server. The Bicep gets a switch to skip the Postgres module and take a connection string instead; the deploy skips the Azure-only database steps (server lookup, firewall rule).
- The deploy workflow becomes build → deploy to staging (automatic) → smoke check → deploy to production (approval). **Production deploys now also require this run's staging smoke check to pass.**
- A pull request branch can be deployed to staging by starting the deploy workflow by hand on that branch. It never deploys to production. No deploy is ever triggered by `pull_request_target`, comments or labels.
- Staging and production have separate concurrency groups, so a staging deploy never cancels a production deploy waiting for approval.
- Staging's database is emptied only when it has a migration the deployed build doesn't (e.g. back on `main` after a pull request that added one); otherwise test data stays.
- Seeded test data: a few password-less test users (sign in through "Forgot password"), circles and wishes, created by the app when it starts on an empty test database. Never a copy of production.
- Email on a test environment is sent only to addresses on an allow-list (exact addresses, whole domains, and `+tag` variants of a listed address), with a `[TEST]` subject prefix. Other mail is dropped and logged with its subject and a masked recipient. If the allow-list is missing on a test environment, nothing is sent.
- A visible "TEST" banner on every page of a test environment, and search engines are told not to index it.
- The monthly reservation job also runs on staging, through the same email allow-list.
- Staging gets its own budget alert on its resource group.

## Capabilities

### New Capabilities
- `staging-environment`: the staging copy of the app: hosting and database, deploys from `main` and pull requests, database reset and seeding, the email allow-list, the TEST marker, and its budget.

### Modified Capabilities
- `hosting`: production deploys also wait for the same build's staging smoke check; CI's Azure sign-in is scoped to the `production` and `staging` environments (each to its own resource group); migrations run as the database's admin login, which on staging is the external database's owner.

## Impact

- **Infra:** `infra/main.bicep` (switch to skip Postgres and take a connection string; environment label and email allow-list settings), `infra/modules/containerapp.bicep` (new app settings), `infra/README.md` (staging setup).
- **Workflows:** `.github/workflows/deploy.yml` (reusable deploy job per environment, staging job, conditional Azure-only database steps, database reset check, separate concurrency), `.github/workflows/infra.yml` (choose the environment; no hard-coded `ca-wishlist`).
- **API:** an email sender wrapper that applies the allow-list; startup reads the environment label; the label reaches the SPA.
- **SPA:** TEST banner; `noindex` on test environments.
- **Seeding:** the app seeds its own test data at start on a test environment whose test users don't exist yet.
- **External:** an account with a free PostgreSQL provider (Neon assumed); a GitHub `staging` environment with its own variables and secrets; an Entra federated credential for it; a staging resource group.
- **Cost:** roughly €0 extra per month (scale-to-zero app, free database, free ACS email domain), watched by the staging budget.
