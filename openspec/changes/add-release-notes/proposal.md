## Why

After a release, everyone sees "New version available — Refresh to get the latest GiftCircles." That doesn't say what changed, so people have no reason to care. From now on every change gets a short description, approved by the user before its PR ("Security fixes" is enough for a security change), and the notice should show it.

## What Changes

- Each release carries a **release note**: one line in the SPA page, `<meta name="release-note" content="…">` in `src/Wishlist.Web/index.html`. Every change that ships app code sets it.
- When the app finds a newer version, it reads that version's release note from the page it just fetched and shows it in the notice under "New version available". The Refresh button works as before.
- With no release note (or an empty one), the notice keeps today's text: "Refresh to get the latest GiftCircles."
- A test fails when `index.html` has no release note, so a release can't go out without one by accident.
- The first release note: "Updates now tell you what's new".

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `ui-foundations`: the update notice shows the deployed version's release note.

## Impact

- **Web**: `index.html` (the meta tag), `lib/appUpdate.ts` (reads the note from the fetched page), `UpdateToaster.tsx` (shows it).
- **Tests**: an API test checks that `index.html` has a non-empty release note and that serving the page keeps it.
- No API or database change.
