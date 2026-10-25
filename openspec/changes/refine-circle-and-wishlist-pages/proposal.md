## Why

The circle page says "Circle" instead of the circle's name, repeats the members as a second "Other wishlists" list, and makes people aim for a small "View wishlist" link. Back buttons on wishlist pages always jump to "My circles", even when you came from a circle, and pages briefly show "No items yet" before their data arrives, which looks like the list was emptied.

## What Changes

- Circle page:
  - The heading shows the circle's name.
  - Members: the signed-in user is listed first. Each member is one full-width button showing just the name; it opens that person's wishlist, or, for yourself, your own wishlist to edit.
  - "Other wishlists" is replaced by "Other circles": the other circles you're in, each opening that circle.
- Back buttons on the wishlist pages (and the circle page) return to the previous screen; with no previous screen in the app (opened from a link), they go to "My circles".
- Lists show a loading placeholder until data arrives, instead of an empty state ("No items yet", "Nothing here yet") that flashes before the real content.
- Fix found while testing: the sign-in page showed "Signing in with Face ID / fingerprint didn't work" on load when its background passkey-autofill request failed before the backend was up (the service worker shows the page instantly during a cold start). That background request now stays silent unless the server rejects a passkey the person chose, and retries a few times if it couldn't start.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: adds requirements for the circle page layout, back navigation and loading states.

## Impact

- **Web**: `GroupDetailPage`, `MyWishlistPage`, `MemberWishlistPage`, a small back-navigation helper.
- **API**: none. The circle name and other circles come from the existing `GET /api/groups/me`; the circle page no longer calls `GET /api/groups/{id}/wishlists`.
