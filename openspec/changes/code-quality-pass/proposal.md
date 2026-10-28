## Why

From the 2026-10-28 code quality review:

1. **Reserve race:** two people reserving the same gift at the same moment. The second one hits the database's one-claim-per-item rule and gets a 500 instead of a clear message.
2. **Duplicated code:** "who is signed in" is copied into six API controllers. Reading the API's error message is copied into eight places in the web app.
3. **One 522 kB script:** every page's code downloads on the first visit, even pages most people rarely open (import, security, recovery).

## What Changes

- **A duplicate reservation gives 409** "Someone else just reserved this gift." The repository turns the unique violation into a `ConflictException`.
- **One `User.GetUserId()` extension** replaces the six controller copies. **One `apiErrorMessage` / `apiErrorStatus` helper** (`api/errors.ts`) replaces the web copies. Behaviour doesn't change.
- **Every page loads on demand** (`lazyPage`, `React.lazy`), inside a `Suspense` in both layouts. The main script drops from 522 kB to 449 kB (162 kB to 146 kB gzipped); what's left is mostly React, the router and UI libraries.
- **The service worker follows the page files:** it caches every chunk the shell's scripts can load, not just the files `index.html` names, and keeps or prunes them the same way. Without this, page files wouldn't be available offline, and old tabs would lose them after a release.
- **Old tab after a release:** if a page's code can't be loaded, the app updates and reloads once. Within 10 seconds it doesn't try again, so it can't get stuck in a reload loop.

## Capabilities

### Modified Capabilities
- `gift-claiming`: concurrent reservations give one winner and a 409.
- `ui-foundations`: pages load on demand; the service worker caches them ahead.

## Impact

- **API**: `GiftClaimRepository.CreateAsync`, `ClaimsPrincipalExtensions`, six controllers.
- **Web**: `App.tsx`, `lib/lazyPage.ts`, `AppShell` / `AuthLayout` (`Suspense`), `api/errors.ts` and its eight callers, `public/sw.js`.
- **Tests**: Postgres race test, which fails without the fix.
