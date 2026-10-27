## Why

Some people don't keep their own wishlist up to date: young children, grandparents, or someone who just finds it a chore. Today only the owner can add wishes, so a parent or partner who knows what they want has no way to put it on their list. Buddies let the owner hand that job to people they trust.

## What Changes

- An owner can make specific people **buddies** for their wishlist. Only people they share at least one circle with can be chosen. The owner can remove a buddy at any time.
- A buddy can **add wishes** to the owner's list from that person's wishlist page ("Add a wish for Sam"). The wish belongs to the owner and goes live right away, with no approval step.
- Every wish added by a buddy shows **"added by <name>"** to the owner and to the givers.
- A buddy has **full control** over the owner's list: they can also **edit and delete any wish** on it, including the ones the owner added. The owner keeps full control too.
- The owner still never sees reservations, and that includes wishes a buddy added.
- Buddy rights only work while the buddy and owner still share a circle. Removing a buddy, or a buddy leaving the shared circles, keeps the wishes they already added.
- A person gets an **email** when they're made a buddy, with a link to the owner's wishlist.
- Out of scope: hidden suggestions the owner can't see (a separate later to-do), an approval flow, and per-circle buddy rights.

## Capabilities

### New Capabilities
- `wishlist-buddies`: managing buddies (list candidates, add, remove, list your buddies and whose buddy you are), the buddy notification email, and adding, editing and deleting wishes on someone else's list as their buddy.

### Modified Capabilities
- `wishlists`: wishes record who added them, and wish responses (own and member wishlist) show "added by". Editing and deleting another user's wish is allowed for their buddies.

## Impact

- **Domain**: new `WishlistBuddy` entity (owner, buddy, created at); `WishlistItem.AddedById` (nullable; null means the owner added it).
- **Infrastructure**: EF Core migration (new table plus a nullable column), repository for buddies, and an email template.
- **Application**: use cases to list buddy candidates, add or remove a buddy, list buddies, and add, edit or delete a wish as a buddy. `WishlistItemDto` gains `AddedBy` (id and display name).
- **API**: `GET/POST/DELETE /api/wishlists/me/buddies`, `GET /api/wishlists/me/buddy-candidates`, `GET /api/wishlists/me/buddy-of` (whose buddy I am), `POST /api/wishlists/{userId}/items`, `PATCH`/`DELETE /api/wishlists/{userId}/items/{itemId}` (buddy adds, edits, deletes).
- **Web**: a "Buddies" dialog on My wishlist, an "Add a wish for X" button plus edit and delete actions on the member wishlist page, and an "added by" label on wish cards.
