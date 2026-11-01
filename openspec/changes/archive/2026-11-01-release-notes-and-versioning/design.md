## Context

- Trunk-based: short branches (often git worktrees, several Claude sessions in parallel) → pull request into `main` → `deploy.yml` builds on every push to `main` and waits for a manual approval on the `production` environment. The `deploy-production` concurrency group runs one deploy at a time; a newer queued run replaces an older pending one.
- Images are tagged `sha-<7>` and `latest`. No version numbers, no git tags, `package.json` is `0.0.0`.
- The release note is `<meta name="release-note">` in `src/Wishlist.Web/index.html`. `scripts/release-note-guard.sh` renames it at build time when it equals the live note; the client reads it from the freshly fetched `index.html` (`releaseNoteOf`).
- The repository is public; release notes are user-facing text and fine to publish.

## Goals / Non-Goals

**Goals:**
- A version number for every production release, starting at 1.0.0, with no manual numbering.
- A release history people can read in the app, built up from 1.0.0 on.
- No merge conflicts between parallel pull requests over notes or versions.
- Git tags and GitHub Releases that mean "this is what went to production".

**Non-Goals:**
- Backfilling notes from before 1.0.0.
- A "new" dot or badge for unseen versions (kept for notifications, #126).
- Release or develop branches, hotfix automation, Conventional Commits.
- Showing the version number in the update notice itself.

## Decisions

### Version = last tag + note files added since
`scripts/release-version.mjs` (run in the deploy build job, with full git history and tags):
1. `last` = highest `vX.Y.Z` tag reachable from `HEAD`. None → the version is `1.0.0`.
2. New note files = files under `release-notes/` (except `README.md`) added between `last` and `HEAD` (`git diff --diff-filter=A --name-only last..HEAD`).
3. Bump = the highest type among them (`major` > `feature` > `fix`); no files → patch.
4. Output the version, and write `releases.json`: for each tag in order, the notes added in its range, plus the new version with today's date (UTC). A tag's date is its tagger date.

Note files stay in the repo forever (one small file per pull request); history is derived from git, so nothing has to be moved or committed back.

*Alternatives:* a single `CHANGELOG.md`/`releases.json` edited in each pull request — conflicts between every two parallel pull requests. A bot commit after each release that moves notes into a history file — writes to `main` behind everyone's back, and would need loop protection. Version in `package.json` bumped per pull request — same conflict problem, and the number would be wrong when deploys are skipped. Conventional Commits — merge commits from GitHub wouldn't carry the type; a file is explicit and reviewable.

### Note file format
`release-notes/<short-slug>.md`:
```
---
type: feature
---
See what's new in each version on the new What's new page
```
`type` is `feature`, `fix` or `major`. The body is one or more lines, each one note (max 200 characters). The slug is free; the branch name is a good default. The check fails on an unknown type, an empty body or a line over 200 characters.

### Versions exist only in the pipeline
`package.json` and the `.csproj` files stay at `0.0.0`. The deploy passes the version to the Docker build as `APP_VERSION` (and the commit as `APP_COMMIT`); the SPA gets `VITE_APP_VERSION`, and the runtime image gets `APP_VERSION` and `APP_COMMIT` environment values that `/healthz` reports. `releases.json` is written to `src/Wishlist.Web/public/` before the Docker build (git-ignored). Locally, the version shows as "dev" and the What's new page shows "No release notes yet" when `releases.json` is missing.

### Tag only after a successful deploy
The deploy job, after the smoke check passes (and `/healthz` reports the expected version), creates an annotated tag `vX.Y.Z` on the run's commit and `gh release create vX.Y.Z` with the notes; it also adds the image tag `X.Y.Z` with `docker buildx imagetools create` (a retag, no rebuild). If the tag already exists (re-run of an old deploy as rollback), these steps are skipped. Needs `contents: write` and `packages: write` on the deploy job.

Because tags only exist for approved deploys, a run that is never approved simply has its notes folded into the next version. Two runs can't race: the concurrency group lets the next build start only after the previous run finished.

*Alternative:* tag at build time — would number releases that never ship.

### Update notice and What's new
- The notice title becomes "A new version of Kadoozle is available", without a release note text, with buttons **Refresh** and **What's new**. `releaseNoteOf` and the meta tag go away.
- **What's new** does what Refresh does, then lands on `/whats-new` (the reload navigates to `/whats-new` instead of the current URL), so the page shows the new version's notes.
- `/whats-new` fetches `/releases.json` (`cache: 'no-cache'`) and lists versions newest first: version, date, notes as bullets. A version without notes shows "Small fixes and improvements". It's reachable signed in (inside the app shell) and signed out (the notice also appears on sign-in pages), with a Back link.
- The service worker fetches `releases.json` network first and keeps the last copy, so the page also works offline.

### Account menu
A "What's new" item above "Sign out", and at the bottom, below a separator, a muted, non-clickable line "Version 1.0.0" ("Version dev" locally).

### CI reminder
The `release-note` job in `ci.yml`: when the pull request changes app code (as today) and adds no file under `release-notes/`, warn "This release ships without a note…"; validate every added/changed note file and fail on a malformed one. The API test project gets a small test that validates every file in `release-notes/` the same way, so a broken file is caught locally too.

### Branching (unchanged, written down)
Short-lived branches → pull request → `main` → approved deploy. A hotfix goes through `main` like anything else. Escape hatch, documented only: if `main` holds work that must not ship yet, branch `hotfix/vX.Y.Z` from the last tag, and run the deploy workflow by hand on it; the script computes a patch version from that tag.

## Risks / Trade-offs

- [Tag push fails after a successful deploy] → the release is live without a tag; the next deploy then counts this release's notes again. Mitigation: tagging is its own step with a clear failure; re-running just that job is safe (skip if the tag exists, tag the run's commit otherwise).
- [Shallow clone gives wrong history] → build checkout uses `fetch-depth: 0` and `fetch-tags: true`.
- [Dependabot releases clutter the page] → they show one line, "Small fixes and improvements"; acceptable, revisit if it gets noisy.
- [Old clients in the field read the meta tag] → after this release the meta tag is gone, so an old client's notice shows its generic fallback text once; harmless.
- [Public GitHub Releases] → notes are the same user-facing text shown in the app; nothing sensitive goes in them (README says so).

## Migration Plan

1. Merge this change with its own note file (`type: major`, though the first version is 1.0.0 regardless).
2. Approve the deploy: the build computes 1.0.0, the deploy tags `v1.0.0` and creates the GitHub Release.
3. Rollback: re-run the deploy job of an earlier run as today; tagging is skipped because that version's tag exists.

## Open Questions

- Wording of the 1.0.0 note — to confirm with the user before the pull request.
