## Why

From the 2026-10-28 code quality review:

- Pull requests get no automatic checks. The deploy workflow runs the API tests and lint, but only after merging, and without a database, so the Postgres tests are skipped. A broken change can reach `main` before anyone notices.
- Workflows use actions by tag (`@v5`). The action's owner, or someone who takes over their account, can move a tag to other code, and the deploy job holds the Azure login. This is how the 2025 `tj-actions/changed-files` attack leaked secrets.
- Nothing keeps dependencies current. The npm advisories found in the review were only fixed by hand.

## What Changes

- **New `ci` workflow on every pull request to `main`:**
  - API tests with a throwaway Postgres 17 service container, so the database tests run too.
  - Web lint and build.
  - No secrets, read-only token, nothing in Azure.
- **Every action is pinned to a commit SHA**, with its version as a comment, in `ci`, `deploy` and `infra`.
- **Dependabot** (`.github/dependabot.yml`) opens update PRs, which `ci` then checks:
  - Actions: weekly.
  - npm (web app) and NuGet (API): monthly.
  - Minor and patch updates are grouped into one PR per ecosystem; each major version gets its own PR.
- No app code changes, so there's no update notice.

## Capabilities

### Modified Capabilities
- `hosting`: pull requests are checked before merging; workflow actions are pinned; dependencies get update PRs.

## Impact

- `.github/workflows/ci.yml` (new), `deploy.yml` and `infra.yml` (pins only), `.github/dependabot.yml` (new).
- Repository settings, done by the user: Dependabot alerts and security updates (both off today), optionally secret scanning, and optionally making `ci` a required check for merging.
