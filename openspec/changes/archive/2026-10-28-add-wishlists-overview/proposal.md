## Why

To open someone's wishlist today you first pick a circle, then find the person in its member list. People think of "Anna's wishlist", not "the Family circle", and someone in several of your circles shows up in each of them. The top menu has "My wishlist" but no way to reach everyone else's lists in one place (to-do #8, "see all people you're connected with, regardless of circle").

## What Changes

- The top menu's **"My wishlist"** becomes **"Wishlists"**, opening a new page at `/wishlists`. It stays highlighted on My wishlist and on anyone's wishlist page.
- The **Wishlists page**:
  - At the top, a row that opens **My wishlist** (`/wishlist/me`).
  - Below it, **everyone you share at least one circle with**, each person once, never yourself, in **alphabetical order**.
  - Under each name, **badges with the circles you share** with them. Long circle names are cut off with "…" (full name on hover), and at most 5 badges show, followed by "+N more", so a person in many circles never breaks the layout.
  - A **filter** box narrows the list by name or circle name. When it matches a circle whose badge would be hidden behind "+N more", that badge is shown.
  - Clicking a person opens their wishlist (`/wishlist/{userId}`), the same page as today.
  - Loading shows placeholders; with no circle-mates it says so and points to My circles.
- **Someone's wishlist page** shows their name in the title ("Anna's wishlist") for everyone who may see it, not only for their buddies.
- The circle page's member list is unchanged: clicking a name still opens that person's wishlist.
- Out of scope: favorites at the top of lists (separate to-do), any change to who may see a wishlist.

## Capabilities

### New Capabilities
- `wishlists-overview`: the list of people the user shares a circle with (API and page), the "Wishlists" menu item, and the person's name on their wishlist page.

### Modified Capabilities
_None._ Access to wishlists (`wishlists`) is unchanged: you can only see the lists of people you share a circle with.

## Impact

- **Application**: `ListConnectedPeopleUseCase` and `ConnectedPersonDto`; a new `IGroupRepository` query for the caller's circle-mates and the circles they share.
- **Infrastructure**: the query in `GroupRepository` (no migration).
- **API**: `GET /api/wishlists/people`.
- **Web**: `WishlistsPage` at `/wishlists`, the menu item in `AppShell`, and the title on `MemberWishlistPage`.
- **Tests**: rules in memory, the endpoint's sign-in check, the query against Postgres.
