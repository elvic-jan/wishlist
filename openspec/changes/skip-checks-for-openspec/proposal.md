## Why

Archive and proposal PRs only change `openspec/`, so nothing in them runs. They still waited several minutes for the API tests and web build, which are now required checks.

## What Changes

- **New `What changed` job in `ci`:** it compares the PR's merge commit with `main`. When only `openspec/` changed, `API tests` and `Web lint and build` are skipped through a job-level `if`. GitHub counts a skipped required check as passed, so the PR stays mergeable.
- Manual runs (`workflow_dispatch`) always run everything.
- No app code changes, so there's no update notice.

## Capabilities

### Modified Capabilities
- `hosting`: OpenSpec-only pull requests skip the checks.

## Impact

- `.github/workflows/ci.yml`.
