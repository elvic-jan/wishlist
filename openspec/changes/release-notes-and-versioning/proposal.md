## Why

Today each release's note is one `<meta name="release-note">` tag in `index.html`, shown inside the "New version available" notice; every new note overwrites the last, so nobody can look back at what changed. Releases also have no number, so there's no way to say which version someone runs or which commit is live. Issue #122.

## What Changes

- **Version numbers.** Kadoozle gets semantic version numbers (MAJOR.MINOR.PATCH), starting at **1.0.0** with this change. A version is created when a deploy reaches production, not when a pull request merges.
- **Release note per pull request.** Each pull request that changes the app adds a small file under `release-notes/` with a type (`feature` → minor, `fix` → patch, `major` → major, set by hand) and a one-line note. One file per pull request, so parallel pull requests never conflict. Replaces the meta tag. **BREAKING** for the workflow: the meta tag and `scripts/release-note-guard.sh` go away.
- **Version and history worked out at deploy.** The deploy build reads the last `v*` git tag and the note files added since, computes the next version, and builds `releases.json` (every version, newest first, with date and notes) into the image. No bot commits to `main`.
- **Tags and GitHub Releases.** After a successful production deploy, the deployed commit is tagged `vX.Y.Z` and a GitHub Release with the notes is created; the image also gets the tag `X.Y.Z`. Re-running an old deploy (rollback) doesn't re-tag.
- **What's new page.** New page `/whats-new` listing versions newest first (version, date, notes). Reachable from the update notice and from the account menu (the menu under your initials). Starts with 1.0.0; past notes are not carried over.
- **Version in the account menu.** The bottom of the account menu shows "Version 1.0.0".
- **Generic update notice.** The notice says "A new version of Kadoozle is available" with **Refresh** and **What's new**; What's new refreshes first, then opens the page in the new version. No "new" dot on the menu (kept free for notifications, #126).
- **CI check.** The pull request reminder looks for a new file in `release-notes/` instead of the meta tag (still a warning, not a failure); a malformed note file fails the check.
- `/healthz` reports the running version and commit, so the deploy's smoke check can confirm the right version is live.

## Capabilities

### New Capabilities
- `releases`: version numbering, release note files, how a deploy turns them into a version, tag, GitHub Release and `releases.json`, the What's new page, and the version shown in the app.

### Modified Capabilities
- `ui-foundations`: "The app picks up new versions after a release" — the notice becomes generic with a What's new link; release notes no longer come from the deployed page's meta tag.

## Impact

- **Web**: `lib/appUpdate.ts`, `components/UpdateToaster.tsx`, `components/AppShell.tsx` (menu item + version), new `pages/WhatsNewPage.tsx` and route, `index.html` (meta tag removed), `vite` build reads `VITE_APP_VERSION`.
- **API**: `/healthz` response body; assembly version set at build; `ReleaseNoteTests` replaced by tests for the note files.
- **Build/deploy**: `Dockerfile` (version build arg), `.github/workflows/deploy.yml` (full history fetch, version step, image retag, tag + GitHub Release, `contents: write` on the deploy job), `.github/workflows/ci.yml` (release-note job), new `scripts/release-version.mjs`, `scripts/release-note-guard.sh` removed.
- **Repo**: new `release-notes/` folder with a README on the file format.
- **Process**: the "every change sets the release note" habit now means adding a note file; Dependabot pull requests add none and ship as a patch version.
