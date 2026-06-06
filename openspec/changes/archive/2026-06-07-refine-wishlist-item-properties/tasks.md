## 1. Domain model

- [x] 1.1 Add `src/Wishlist.Domain/Entities/WishlistItemUrl.cs` with `Id (Guid)`, `WishlistItemId (Guid)`, `Url (string)`, `DisplayOrder (int)`
- [x] 1.2 In `src/Wishlist.Domain/Entities/WishlistItem.cs`, remove the `Url` property, remove the `Price` property, and add `IList<WishlistItemUrl> Urls { get; set; } = new List<WishlistItemUrl>();` as the navigation collection

## 2. Persistence

- [x] 2.1 In `src/Wishlist.Infrastructure/Persistence/WishlistDbContext.cs`, register `DbSet<WishlistItemUrl> WishlistItemUrls`
- [x] 2.2 In `OnModelCreating`, configure `WishlistItemUrl`: PK on `Id`, required `Url` (max length 2048), required `WishlistItemId` (FK), required `DisplayOrder`, a composite index on `(WishlistItemId, DisplayOrder)`, and `OnDelete(DeleteBehavior.Cascade)` from `WishlistItem`
- [x] 2.3 Drop the `Url` and `Price` column mappings from the `WishlistItem` configuration
- [x] 2.4 Generate EF migration: `dotnet ef migrations add RefineWishlistItemUrlsAndDropPrice --project src/Wishlist.Infrastructure --startup-project src/Wishlist.API`
- [x] 2.5 Review the generated migration: drops `Price` and `Url` columns, creates `WishlistItemUrls` with PK + FK (cascade) + composite index. `Down()` recreates the dropped columns as nullable. No backfill, as designed
- [x] 2.6 Apply: `dotnet ef database update --project src/Wishlist.Infrastructure --startup-project src/Wishlist.API`

## 3. Repository

- [x] 3.1 In `src/Wishlist.Infrastructure/Repositories/WishlistRepository.cs`, update `GetByOwnerAsync` and `GetByIdAsync` to `Include(w => w.Urls.OrderBy(u => u.DisplayOrder))` so the navigation collection comes back in submission order
- [x] 3.2 Update `CreateAsync` signature: take `IReadOnlyList<string> urls` (already validated) and instantiate one `WishlistItemUrl` per URL with `DisplayOrder = index`
- [x] 3.3 Add `ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, ...)`: bulk-delete existing rows via `ExecuteDeleteAsync`, detach the stale tracked copies, then `db.WishlistItemUrls.Add(...)` the new rows one-by-one with explicit `WishlistItemId`. EF's relationship fixup populates the navigation; manually appending to `item.Urls` would duplicate the in-memory collection in the response
- [x] 3.4 Confirmed cascade-delete on `WishlistItem` removes orphan `WishlistItemUrl` rows (verified via DB row count after `DELETE` endpoint)

## 4. Application layer (DTOs + use cases)

- [x] 4.1 In `src/Wishlist.Application/DTOs/WishlistDtos.cs`, change `AddWishlistItemRequest` to `(string Title, string? Description, IReadOnlyList<string>? Urls)` — dropped `Url` and `Price`
- [x] 4.2 Change `EditWishlistItemRequest` to `(string? Title, string? Description, IReadOnlyList<string>? Urls)` — dropped `Url` and `Price`
- [x] 4.3 Change `WishlistItemDto` to include `IReadOnlyList<string> Urls`; dropped `Url` and `Price`
- [x] 4.4 Add `WishlistItemUrlValidator.Validate(...)` in `Wishlist.Application/UseCases/Wishlists/`: returns empty list for null input; rejects > 10 URLs; trims each entry; rejects empty/whitespace; rejects when scheme isn't http/https; throws `DomainException` (mapped to 400 by `ErrorHandlingMiddleware`)
- [x] 4.5 In `AddWishlistItemUseCase`, call the validator and pass the result to the repository's create path; map the persisted `Urls` collection back to a `string[]` on the response
- [x] 4.6 In `EditWishlistItemUseCase`, validate URLs up front (so a bad URL doesn't half-persist a title change), then call `ReplaceUrlsAsync` if `Urls` was present, otherwise the plain `UpdateAsync`
- [x] 4.7 Updated `GetOwnWishlistUseCase`, `GetMemberWishlistUseCase`, `ReserveGiftUseCase`, `MarkGiftAsBoughtUseCase` to project `item.Urls` into the DTO's `Urls` field (they all construct `WishlistItemDto` and were caught by the build)

## 5. API

- [x] 5.1 `WishlistsController` is pass-through; no controller-level logic depends on `Url` or `Price`. No changes needed
- [x] 5.2 Confirmed default JSON serializer produces `urls: [...]` with the new shape; no manual response shaping needed

## 6. SPA

- [x] 6.1 Updated TypeScript types inline in both pages: `urls: string[]` on the WishlistItem interface; dropped `url`/`price`
- [x] 6.2 In `src/Wishlist.Web/src/pages/MyWishlistPage.tsx`, replaced the single URL `<input>` with a URL-list editor:
  - One row per URL with a "✕" remove button
  - "+ Add link" button, disabled at 10 URLs (also shows `n/10` counter)
  - Client-side check: trim + URL constructor + scheme in {http,https}; inline error for >10 or malformed
- [x] 6.3 Removed the price `<input>` from the same form
- [x] 6.4 In both `MyWishlistPage.tsx` and `MemberWishlistPage.tsx`, render each URL as its own `<a href={u} target="_blank" rel="noopener noreferrer">{hostFor(u)}</a>` where `hostFor` strips `www.` from `new URL(url).host`. Empty `urls` array renders nothing in the URLs row
- [x] 6.5 Removed price display from both pages

## 7. Verification

- [x] 7.1 `dotnet build src/Wishlist.API` succeeds with 0 errors, 0 warnings
- [x] 7.2 Register a user, add a wishlist item with 0 URLs → 200 with `urls: []`
- [x] 7.3 Add an item with 3 URLs in a specific order; fetch → URLs returned in submitted order; DB `DisplayOrder` matches positions 0, 1, 2
- [x] 7.4 Add an item with 11 URLs → HTTP 400 `{"error":"A wishlist item may have at most 10 URLs."}`
- [x] 7.5 Add an item with `["javascript:alert(1)"]` → HTTP 400 `{"error":"URL must be an absolute http or https URL."}` (same error for `["/relative/path"]`)
- [x] 7.6 Edit an existing 3-URL item to a 1-URL list → DB shows exactly 1 row, no orphans; API response shows just that one URL (no duplicates)
- [x] 7.7 Delete an item with URLs → cascade removes the URL rows (DB count = 0 after DELETE)
- [x] 7.8 Verified clickable URLs render in both `MyWishlistPage` and `MemberWishlistPage` via the rewritten components (typecheck-clean; runtime exercised via the same API the SPA calls)
- [x] 7.9 Edit rejection cases (11 URLs / `javascript:` / relative) leave the existing URL list unchanged in DB (DB count unchanged after rejected PATCH)

## 8. Cleanup

- [x] 8.1 `grep` for `\.Price\b|\.Url\b|item\.url\b|item\.price\b|price\?:|url\?:` across `src/` returns only legitimate references on the new `WishlistItemUrl` entity's `Url` property (no stale references to the dropped fields)
