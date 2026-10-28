## 1. Checks

- [x] 1.1 `ci.yml`: on pull requests to `main`; API tests with a `postgres:17` service and `GIFTCIRCLES_TEST_DB`; web `npm ci`, lint, build; read-only token
- [x] 1.2 The workflow runs green on this change's own pull request (all 160 tests, Postgres included)

## 2. Supply chain

- [x] 2.1 Pin every action in `ci`, `deploy` and `infra` to the commit of its latest release in the current major
- [x] 2.2 `.github/dependabot.yml`: actions weekly, npm and NuGet monthly, minor/patch grouped

## 3. Repository settings (user)

- [x] 3.1 Turn on Dependabot alerts and security updates (and, optionally, secret scanning) — handed to the user; still off on 2026-10-28
- [x] 3.2 Optionally require the `ci` checks before merging — handed to the user; not required on 2026-10-28
