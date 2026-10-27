## 1. Domain and database

- [x] 1.1 `WishlistBuddy` entity (OwnerId, BuddyId, CreatedAt), unique on (OwnerId, BuddyId)
- [x] 1.2 `WishlistItem.AddedById` (nullable) plus an `AddedBy` navigation to the user
- [x] 1.3 EF Core config and migration: `WishlistBuddies` table (cascade on user delete) and the `WishlistItems.AddedById` FK (`SET NULL`)

## 2. Application

- [x] 2.1 `IWishlistBuddyRepository`: add (idempotent), remove, exists, list buddies / owners-I-am-buddy-for (each filtered to a shared circle), list candidates (shared circle members minus self and buddies)
- [x] 2.2 `WishlistItemDto` gains `AddedBy` (`AddedByDto(UserId, DisplayName)`); fill it in the own-list, member-list, add and edit use cases (repository loads the adder)
- [x] 2.3 `ManageBuddiesUseCase`: list buddies, list candidates, list buddy-of, add (403 without shared circle, 400 for self, email best-effort only when newly added), remove
- [x] 2.4 Buddy email text (plain text + HTML, like the reservation emails) linking to `/wishlist/{ownerId}`
- [x] 2.5 `WishlistAccess.CanManageAsync(callerId, ownerId)` (owner, or buddy + shared circle); add/edit/delete use cases take a target owner id and share one code path for `/me` and buddy routes (add sets `AddedById` when caller ≠ owner; 400 when a buddy route targets yourself; 404 when the item isn't on that owner's list)
- [x] 2.6 What's-new message announcing buddies (Application/Messages)

## 3. API

- [x] 3.1 `GET/POST /api/wishlists/me/buddies`, `DELETE /api/wishlists/me/buddies/{userId}`, `GET /api/wishlists/me/buddy-candidates`, `GET /api/wishlists/me/buddy-of`
- [x] 3.2 `POST /api/wishlists/{userId}/items`, `PATCH`/`DELETE /api/wishlists/{userId}/items/{itemId}`

## 4. Tests

- [x] 4.1 API tests for the buddy endpoints: add/remove/list, no shared circle, self, idempotent add (single email)
- [x] 4.2 API tests for buddy add/edit/delete: success (incl. owner-added wishes, delete removes claim), `addedBy` set on add and unchanged on edit, non-buddy 403, buddy after leaving circles 403, wrong owner 404, validation 400
- [x] 4.3 Owner still sees no claim on a reserved buddy-added wish

## 5. Web

- [x] 5.1 API client: buddy endpoints, add/edit/delete for an owner, `addedBy` on the item type
- [x] 5.2 My wishlist: "Buddies" menu entry and dialog (current buddies with Remove, "Add buddy" picker from candidates)
- [x] 5.3 Member wishlist page: when the owner is in `buddy-of`, "Add a wish for <name>" plus edit and delete on every wish (reuse the existing form and confirmation; the confirmation warns when the wish is reserved)
- [x] 5.4 Wish cards: "added by <name>" / "added by you"
- [x] 5.5 Check on phone width and in dark mode
