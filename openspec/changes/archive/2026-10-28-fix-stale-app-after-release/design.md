## Context

The service worker (`public/sw.js`) serves the cached `index.html` for every navigation and refreshes it in the background (stale-while-revalidate), so the app shell appears instantly even while the container cold-starts (~20 s). Assets under `/assets/` are content-hashed by Vite and served cache-first. The server sends no `Cache-Control` for static files or for the SPA fallback page (only `/join/{code}` is `no-store`).

## Goals / Non-Goals

**Goals:**
- People find out about a new version without refreshing by hand, and get it with one tap.
- A message action never lands in an app that's too old for it.
- Keep the instant start during a cold start.

**Non-Goals:**
- Reloading automatically. The user picked a notice so nothing reloads while they're typing.
- Listing what changed in the notice itself. The what's-new message shows that after the refresh.
- A separate version endpoint or build number. The page itself is the version.
- Old clients that can't talk to a changed API. Not needed today; the notice makes it unlikely.

## Decisions

**1. The deployed `index.html` is the version.**
Vite puts the entry script's hashed name (`/assets/index-<hash>.js`) in `index.html`, so a new build means a new name. The app fetches `/index.html` with `cache: 'no-cache'` and compares the entry script in it with the one it is running (`document.querySelector('script[type=module][src*="/assets/"]')`). The service worker doesn't intercept `/index.html` fetches from the page (only navigations and `/assets/`), so this goes to the server. A 304 costs a few hundred bytes. *Alternative:* a `/api/version` endpoint. That needs a build number threaded through the Dockerfile and deploy workflow and says the same thing.

**2. When the app checks.**
On start (after load), and when the page becomes visible again (`visibilitychange`), at most once a minute. A check that is already running is reused. A failed or slow check (server unreachable, cold start) is ignored: the user keeps the version they have, and the next check tries again. On the same visibility events the app also calls `registration.update()`, so a new `sw.js` installs too. Production builds only.

**3. Refresh asks the service worker first.**
After a long-open app finds a new version, the worker's cached page may still be old, and a plain reload would show it again. So Refresh posts `{ type: 'refresh-shell' }` to the active worker over a `MessageChannel`. The worker runs its normal shell refresh (fetch, cache, prune) and replies. The app then reloads, or goes to a target URL with `location.assign`. If the worker doesn't answer within 5 s, or there is no worker, it reloads anyway.

**4. The notice is a lasting toast like the messages.**
The card (icon, title, text, close, buttons, bottom-right placement) moves into a shared `LastingToast` component used by `MessageToaster` and the new `UpdateToaster`. `UpdateToaster` sits in `App`, so it also shows on the sign-in pages. While it shows, `MessageToaster` stays hidden, so the two never overlap: a message is about the new version and should be read after the refresh. The notice has only a Refresh button, no "Not now" or close: putting it off would leave people on an outdated version with inconsistent behaviour, and a reload is quick. Text: "New version available" / "Refresh to get the latest GiftCircles."

**5. Message actions check first.**
The action button awaits the update check (at most 3 s, reusing a running check). If a newer version is deployed, it goes there through the Refresh path of decision 3 with the action's URL; otherwise it navigates inside the app as today. The message is dismissed first either way.

**6. Cache headers.**
- `index.html` (as a static file) and the SPA fallback page: `Cache-Control: no-cache`.
- `/sw.js`: `no-cache`. Browsers already bypass the HTTP cache for worker scripts, but proxies might not.
- `/assets/*`: `public, max-age=31536000, immutable`.
- `/join/{code}`: stays `no-store` (personal).
- Icons, manifest, og image: unchanged.

Static file headers are set in `StaticFileOptions.OnPrepareResponse`; the fallback page uses a `SpaPage.Html` cache option.

**7. The worker's cache stays consistent.**
Found while testing: the background shell refresh stored a new `index.html` without its assets (they were only fetched when that page first ran). With two releases between visits, the cached page named files the server had already dropped, and the app came up blank. Now:
- The shell refresh caches the new page's missing assets first and only then stores the page. If the assets can't be fetched, it keeps the old page.
- `install` does the same (assets first), so the first release with this change also repairs caches left in that state.
- Cache lookups use `ignoreVary` (hashed files don't vary; a `Vary: Origin` from a server made lookups miss in local testing).
- `cacheFirst` never stores an HTML response for an asset, so a server's fallback page can't be cached as a script.

## Risks / Trade-offs

- **The first release with this change** can't announce itself; it arrives one visit late as before.
- **Notice right after sign-in or mid-edit:** it never interrupts, it only waits in the corner.
- **No-SW browsers** (first visit, private window) now always revalidate the page: a 304 on each full page load, cheap.
- **Background download per release:** the worker downloads a new release's assets (~520 kB, ~160 kB compressed) when it sees the new page, instead of when the page first runs. Same total, just earlier.
