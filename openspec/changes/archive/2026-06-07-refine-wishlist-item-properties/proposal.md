## Why

Two friction points have shown up in the wishlist item model that this change cleans up before more features pile on top:

1. **One URL per item is too few.** Real wishlist items often live in more than one place — the Amazon listing, a local shop, an alternate model on the manufacturer's site. Today the owner has to pick one and either bury the others in `Description` (lossy, not clickable) or drop them entirely. Gifters end up doing detective work.
2. **`Price` is a single rigid decimal that nobody really uses.** It cannot express "around $30," "see link," or "no idea" — which is what owners actually want to say. In practice the field ends up empty or filled with rough guesses; nothing currently consumes it for sorting, filtering, budgeting, or anything else. Better to drop it cleanly than to keep a half-true datum around.

There is no production user data yet, so both schema changes are safe to land as one migration without backfill.

## What Changes

- Replace the single nullable `Url` field on `WishlistItem` with an **ordered list of up to 10 URLs**. URLs are stored in a new `WishlistItemUrl` join entity that preserves the owner's input order. Each URL is rendered as its own clickable link in the SPA, using the URL's host as link text by default (e.g. `amazon.com`).
- **Remove the `Price` field** from `WishlistItem`, its EF column, all DTOs, and the SPA forms/displays. The column is dropped in the same migration.
- Update the `wishlists` capability spec to reflect both shape changes: the create / view-own / edit requirements drop `price` and turn `URL` into `URLs`.
- Validation rules:
  - Each URL MUST be a syntactically valid absolute HTTP/HTTPS URL.
  - The list MUST contain at most 10 URLs.
  - Items with zero URLs remain valid (URLs stay optional).

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `wishlists`: the existing "Add wishlist item", "View own wishlist", and "Edit wishlist item" requirements change shape — `URL` becomes `URLs` (ordered list, max 10) and `price` is dropped entirely. Other requirements in the capability (delete, view-another-member, list-in-group) are unaffected by the field change.

## Impact

- **Affected code**:
  - `src/Wishlist.Domain/Entities/WishlistItem.cs` — drop `Price`, drop `Url`, add navigation property `Urls`
  - `src/Wishlist.Domain/Entities/WishlistItemUrl.cs` (new) — `Id`, `WishlistItemId`, `Url`, `DisplayOrder`
  - `src/Wishlist.Infrastructure/Persistence/WishlistDbContext.cs` — register the new entity, configure ordered child collection
  - `src/Wishlist.Infrastructure/Migrations/` — new EF migration: create `WishlistItemUrls` table, drop `Price` and `Url` columns
  - `src/Wishlist.Infrastructure/Repositories/WishlistRepository.cs` — load + persist the `Urls` collection in the existing read/write paths
  - `src/Wishlist.Application/DTOs/WishlistDtos.cs` — `AddWishlistItemRequest`, `EditWishlistItemRequest`, `WishlistItemDto`: drop `Price` and `Url`, add `Urls: List<string>?`
  - `src/Wishlist.Application/UseCases/Wishlists/AddWishlistItemUseCase.cs` and `EditWishlistItemUseCase.cs` — enforce the per-URL validation + the ≤10 cap
  - `src/Wishlist.API/Controllers/WishlistsController.cs` — pass-through (no contract change beyond the DTO shape)
  - `src/Wishlist.Web/src/pages/MyWishlistPage.tsx` and `MemberWishlistPage.tsx` — render a clickable list of URLs, drop price display
  - `src/Wishlist.Web/src/pages/MyWishlistPage.tsx` (add/edit form) — replace single URL input + price input with a small URL-list editor (add row, remove row); enforce ≤10 client-side as well
- **Data**: drop columns `Url` and `Price` from `WishlistItems`; create new `WishlistItemUrls` table. No backfill (no real data).
- **API contract**: `WishlistItemDto` adds `urls: string[]`, drops `url` and `price`. **Breaking** for any consumer of the existing JSON shape — acceptable because the only consumer today is the in-repo SPA, which lands in the same change.
- **SPA contract**: forms grow a small URL-list editor; item card grows a list of clickable host-text links.
- **Dependencies**: none added or removed.
