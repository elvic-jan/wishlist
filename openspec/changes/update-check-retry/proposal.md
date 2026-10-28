## Why

On the installed Android app, the user saw no "New version available" notice after a release. The notice only appeared after a manual refresh, which first showed "Waking up the server…".

The app checked for a new version once at start, and again only when it came back to the foreground. When that first check failed, nothing retried while the app stayed open. It could fail because Android resumed the app before its network was back, or because the server was still waking from zero. In the browser this rarely shows, because tabs are opened and refocused more often.

## What Changes

- **A failed check is retried** after 10 s, 30 s and 1 min, then every 5 min while the app is open.
- **More moments to check:** when the device comes back online, when the page is restored from the browser's page cache, and every 15 minutes while the app is open (besides at start and on returning to the foreground).
- A single check gives up after 45 s instead of waiting indefinitely.
- **Release note:** "More reliable update notices". The fix only helps from the next release after this one, because the version people have now still has the old check.

## Capabilities

### Modified Capabilities
- `ui-foundations`: update checks retry and repeat while the app is open.

## Impact

- `src/Wishlist.Web/src/lib/appUpdate.ts` (`startUpdateChecks`, `checkForUpdate`), `index.html` (release note).
