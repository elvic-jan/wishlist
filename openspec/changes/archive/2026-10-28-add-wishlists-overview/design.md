## Context

Circles (`Groups`) and memberships (`GroupMembers`) decide who may see whose wishlist: `GetMemberWishlistUseCase` checks `SharesGroupAsync` and returns 403 otherwise. The SPA reaches a member's wishlist only from a circle page (`GroupDetailPage` → `/wishlist/{userId}`). `MemberWishlistPage` gets the items from `GET /wishlists/{userId}`, which carries no owner name, so its title is just "Wishlist" unless the viewer is a buddy (the buddy list has the name). The buddy picker already has a "distinct circle-mates" query, but without circle names and minus existing buddies.

## Goals / Non-Goals

**Goals:**
- One page listing everyone the user shares a circle with, each once, with the shared circles, filterable.
- "Wishlists" in the top menu, covering My wishlist and other people's lists.
- The owner's name in the title of their wishlist page.

**Non-Goals:**
- Favorites at the top (separate to-do).
- Changing who may see a wishlist, or showing wish counts or other details per person.

## Decisions

**1. A new endpoint, `GET /api/wishlists/people`.**
It returns `[{ userId, displayName, circles: [{ id, name }] }]`: every user who shares at least one circle with the caller, the caller excluded, sorted by display name (case-insensitive, then id), each person's circles sorted by name. *Alternative:* the client calls `/groups/me` and then `/groups/{id}/members` for each circle and merges. That is one request per circle and puts the "who am I connected with" rule in the browser. The endpoint keeps it next to the access rule and is a single query. It lives in `WishlistsController` because the page is about wishlists; the guid route constraint on `wishlists/{userId:guid}` keeps `people` from clashing.

**2. The repository returns flat rows; the use case groups them.**
`IGroupRepository.GetCircleMatesAsync(userId)` returns one row per (person, shared circle): user id, display name, circle id, circle name, from a single join of the caller's memberships with the other memberships of those circles. `ListConnectedPeopleUseCase` groups by person and sorts. The grouping and ordering rules then sit in the application layer and are tested in memory; the Postgres test covers the query.

**3. The page filters on the client.**
The list is small (family and friends), so the page loads it once and filters as you type: a person matches when the filter is part of their name or of one of their shared circles' names, ignoring case and accents. No server-side search.

**4. Badges stay bounded.**
Each badge is cut off at a fixed width with "…" and has the full circle name as its tooltip (`title`). A person shows at most 5 badges, then a "+N more" badge whose tooltip lists the rest. With the width cap this is at most about three lines on a phone and one or two on a desktop. When the filter matches circles, the matching badges come first, so a match is never hidden behind "+N more".

**5. The member wishlist title uses the same endpoint.**
`MemberWishlistPage` also loads `/wishlists/people` and takes the person's name from it ("Anna's wishlist"). If the person isn't in the list (no shared circle, the request fails, or it's still loading) the title stays "Wishlist", as today. *Alternative:* add the owner's name to `GET /wishlists/{userId}`. That changes the response from a list to an object for every caller; reusing the people list is a smaller change.

**6. Menu highlighting.**
The "Wishlists" link points to `/wishlists` and is marked active for `/wishlists` and any `/wishlist/...` path (My wishlist and other people's lists), using the current location rather than `NavLink`'s own path match.

## Risks / Trade-offs

- [Someone in very many circles] → width-capped badges, at most 5 plus "+N more".
- [Large circles make the list long] → the filter; still one request, fine at family scale.
- [The title needs a second request on the member page] → small; the title shows "Wishlist" until it arrives.
