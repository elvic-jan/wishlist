## 1. Release note files

- [ ] 1.1 Add `release-notes/README.md` describing the file format (type, one note per line, ≤200 characters, user-facing and public)
- [ ] 1.2 Add this change's own note file `release-notes/release-notes-and-versioning.md` (wording confirmed with the user)
- [ ] 1.3 Replace `ReleaseNoteTests` with a test that validates every file in `release-notes/`

## 2. Version script

- [ ] 2.1 Write `scripts/release-version.mjs`: last `v*` tag, added note files since, bump rules, 1.0.0 when no tag; prints the version and notes, writes `releases.json`
- [ ] 2.2 Test the script locally on a scratch repo: no tag → 1.0.0; feature + fix → minor; no notes → patch; history across several tags
- [ ] 2.3 Remove `scripts/release-note-guard.sh`; git-ignore `src/Wishlist.Web/public/releases.json`

## 3. Build and API

- [ ] 3.1 Dockerfile: `APP_VERSION` and `APP_COMMIT` build args → `VITE_APP_VERSION` for the SPA, `-p:Version=` for the API, `APP_COMMIT` env in the runtime image
- [ ] 3.2 `/healthz` returns `{ version, commit }` (no database query); update its tests

## 4. Web app

- [ ] 4.1 Remove the release-note meta tag from `index.html` and `releaseNoteOf`/`useUpdateNote` from `lib/appUpdate.ts`
- [ ] 4.2 Add a way to apply the update and land on `/whats-new`
- [ ] 4.3 `UpdateToaster`: title "A new version of Kadoozle is available", buttons Refresh and What's new
- [ ] 4.4 `WhatsNewPage` at `/whats-new` (lazy, works signed in and signed out): fetch `/releases.json`, list newest first, "Small fixes and improvements" for empty releases, empty state locally
- [ ] 4.5 Service worker precaches `releases.json`
- [ ] 4.6 Account menu: "What's new" item, and a muted "Version X.Y.Z" line at the bottom ("Version dev" locally)
- [ ] 4.7 Lint and build pass; check the menu, the page and the notice in the browser (narrow and wide)

## 5. Workflows

- [ ] 5.1 `ci.yml` release-note job: warn when app code changes without a new `release-notes/` file; fail on malformed note files
- [ ] 5.2 `deploy.yml` build: full history and tags, run `release-version.mjs`, pass version/commit to the Docker build, expose the version as a job output
- [ ] 5.3 `deploy.yml` deploy: smoke check verifies the version from `/healthz`; then, unless the tag exists, tag the commit, create the GitHub Release, add the `X.Y.Z` image tag; grant `contents: write` and `packages: write`
- [ ] 5.4 Update the hosting docs (`infra/README.md` or equivalent): versioning, tags, rollback, hotfix escape hatch

## 6. Ship

- [ ] 6.1 Open the pull request ("Closes #122") with the confirmed note text
- [ ] 6.2 After the deploy: check `v1.0.0` tag, GitHub Release, version in the menu and the What's new page on www.kadoozle.app
