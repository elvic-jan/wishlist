## 1. Service worker

- [x] 1.1 `sw.js` answers a `refresh-shell` message: runs the shell refresh and replies when done (also on failure)
- [x] 1.2 Shell refresh and install cache a page's assets before the page; lookups ignore `Vary`; HTML is never cached as an asset

## 2. Update check

- [x] 2.1 `lib/appUpdate.ts`: compare the deployed `index.html` entry script with the running one; check on start and when visible again (at most once a minute, reuse a running check, ignore failures); call `registration.update()` on the same events; production only
- [x] 2.2 `applyUpdate(url?)`: ask the worker to refresh the shell (5 s timeout), then reload or go to the URL

## 3. Notice

- [x] 3.1 Shared `LastingToast` card; `MessageToaster` uses it
- [x] 3.2 `UpdateToaster` in `App`: "New version available", Refresh only (no Not now or close)
- [x] 3.3 `MessageToaster` hidden while the update notice shows

## 4. Message actions

- [x] 4.1 Action awaits the update check (max 3 s); newer version → `applyUpdate(url)`, otherwise in-app navigation

## 5. Server cache headers

- [x] 5.1 `index.html`, `sw.js`: `no-cache`; `/assets/*`: `public, max-age=31536000, immutable`
- [x] 5.2 SPA fallback page: `no-cache`; `/join/{code}` stays `no-store`
- [x] 5.3 Tests for the headers

## 6. Verification

- [x] 6.1 API tests, SPA lint and build
- [x] 6.2 Browser check with a production build: notice appears after a simulated release (also on the sign-in page), Refresh loads the new version even when the service worker had cached an older one; two releases in a row (old files removed, no visit in between, also right after a fresh install) still open the app and Refresh loads the newest; a cache broken by the old worker is repaired by the new one; headers covered by API tests. Message action path not exercised in the browser (needs a signed-in session with the API)
- [x] 6.3 After the next release (user): the notice appears on the phone and Refresh shows the new version
