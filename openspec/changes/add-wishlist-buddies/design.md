## Context

Wishes (`WishlistItem`) have a single `OwnerId`, and only the owner can add, edit or delete them through `/api/wishlists/me/items`. Givers read someone's list through `GET /api/wishlists/{userId}`, which requires a shared circle (`IGroupRepository.SharesGroupAsync`). `WishlistItem.GetClaimVisibleTo` hides claims from the owner. Emails go through `IEmailSender`, using the same plain-text and HTML style as the reservation and account emails.

## Goals / Non-Goals

**Goals:**
- An owner chooses buddies among people they share a circle with, and can remove them.
- A buddy has full control over the owner's list (add, edit, delete any wish). Added wishes belong to the owner, and everyone sees "added by".
- Reservations stay hidden from the owner, including on buddy-added wishes.

**Non-Goals:**
- Hidden suggestions (wishes the owner can't see). That is a separate later to-do, but this design shouldn't block it.
- Approval of buddy wishes, per-circle rights, and a change history of who edited what.

## Decisions

**1. A buddy-added wish is a normal wish owned by the owner, with `AddedById`.**
`OwnerId` stays the owner, so the claim hiding works without changes. `AddedById` is nullable: null means the owner added it, which also covers every existing row. *Alternative:* a separate "suggestion" table. That would mean duplicating the read, claim and delete paths for no gain in this version.

**2. A separate `WishlistBuddies` table (OwnerId, BuddyId, CreatedAt), unique on the pair.**
Rights are per person and directional: Sam being Jan's buddy doesn't make Jan Sam's buddy. *Alternative:* a flag on group membership. That would make the rights per circle, which the user ruled out.

**3. Rights need the buddy relationship *and* a shared circle, checked on each request.**
Leaving a circle doesn't delete the buddy row, so rights come back if the two people share a circle again. This keeps leave-circle logic untouched, and stale rows are harmless. Lists filter out buddies who no longer share a circle, so the owner never sees a buddy they can't reach.

**4. Removing a buddy keeps their wishes.**
The wishes belong to the owner. `AddedById` stays and points to a user who still exists, and the label keeps showing who added the wish. If a user account is ever deleted, the FK is `SET NULL`.

**5. API shape.**
- `GET /api/wishlists/me/buddies`: my buddies (id, display name).
- `GET /api/wishlists/me/buddy-candidates`: people who share a circle with me, minus me and minus my existing buddies.
- `POST /api/wishlists/me/buddies` `{ userId }`: add a buddy. Returns 403 without a shared circle. Adding someone who is already a buddy does nothing (no second email).
- `DELETE /api/wishlists/me/buddies/{userId}`: remove a buddy (204, even if they weren't one).
- `GET /api/wishlists/me/buddy-of`: whose lists I can add to. The member wishlist page uses this to show the button. *Alternative:* wrap the member wishlist response with `canAddItems`. That changes an existing response shape for one boolean, so a separate small endpoint is cleaner.
- `POST /api/wishlists/{userId}/items`, `PATCH /api/wishlists/{userId}/items/{itemId}`, `DELETE /api/wishlists/{userId}/items/{itemId}`: a buddy adds, edits or deletes any wish on the owner's list, with the same bodies, validation and effects as the `/me/items` endpoints. Deleting also removes any claim, as it does for the owner. They return 403 if the caller isn't a buddy with a shared circle, and 404 if the item isn't on that owner's list. If `userId` is the caller, they return 400, because owners use `/me/items`.

**5b. One permission check, shared by all three.**
A single `WishlistAccess.CanManageAsync(callerId, ownerId)` (owner, or buddy with a shared circle) backs the buddy endpoints. The add, edit and delete use cases take the target owner id, so the `/me` and buddy routes share one code path instead of duplicating validation. The owner keeps full control of the list alongside their buddies.

**6. `WishlistItemDto` gains `AddedBy: { userId, displayName } | null`.**
The repositories load the adder's display name with the items. The owner's own list and the member list both send it.

**7. The email is best-effort.**
The buddy row is saved first. If sending the "You can now add wishes for X" email fails, the failure is logged and the request still succeeds, the same as the reservation emails. The email links to `/wishlist/{ownerId}`.

**8. Web.**
- **My wishlist**: the menu gets a "Buddies" entry that opens a dialog. The dialog lists current buddies with a Remove button and has an "Add buddy" picker filled from the candidates.
- **Member wishlist page**: if the owner is in `buddy-of`, show an "Add a wish for <name>" button and the edit and delete actions on every wish. They reuse the existing add/edit form and delete confirmation, pointed at the buddy endpoints. The buddy is also a giver, so they see reservations. When they delete a wish someone reserved, the confirmation says so.
- **Wish cards**: show a small "added by <name>" line when `addedBy` is set. When the viewer added the wish, it reads "added by you".
- A what's-new message (Application/Messages) announces the feature.

## Risks / Trade-offs

- [Spam or unwanted wishes from a buddy] → Only people the owner picked can add wishes. The owner can delete any wish and remove the buddy at any time.
- [A buddy edits or deletes a wish the owner wanted to keep] → Buddies are people the owner trusts, and the owner can remove them at any time. There's no change history in this version.
- [A buddy deletes a reserved wish, and the giver who reserved it loses it] → The buddy sees the reservation and gets a warning in the delete confirmation. Notifying the giver belongs to to-do #6 (the reserved-wish deletion flow).
- [Stale buddy rows after leaving circles] → They're harmless, because every check also requires a shared circle.
- [The extra `buddy-of` request on the member page] → It's one small query, cached by the existing client fetch layer.

## Migration Plan

One EF migration adds the `WishlistBuddies` table and a nullable `WishlistItems.AddedById` (FK to Users, `SET NULL`). Existing rows stay null, so no backfill is needed. It deploys with the normal pipeline. To roll back, revert the app; the extra column and table are ignored.

## Open Questions

- None blocking. A later "hidden suggestions" change can add a visibility flag to the buddy-added wish.
