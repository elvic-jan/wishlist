## Context

`WishlistItem` is a small EF-mapped entity (`Id`, `OwnerId`, `Title`, `Description?`, `Url?`, `Price?`, `CreatedAt`, `UpdatedAt`, plus a one-to-zero-or-one `GiftClaim?` navigation). The application layer treats it as a record with the same fields via `WishlistItemDto`. The SPA's wishlist pages render those fields and submit add/edit forms with the same shape.

The two property changes — turning the single `Url` into an ordered list, and removing `Price` outright — are pure data-model shape changes. They do not touch authentication, the claim model, the group membership rules, or any other capability. They DO break the JSON contract returned by the wishlist endpoints, which is acceptable because the only consumer today is the in-repo SPA, which lands in the same change.

There is no production data yet, so the migration is a destructive drop on `Price` (and on `Url`, since its data moves into the new table only when there is data to move — confirmed: there is none).

## Goals / Non-Goals

**Goals:**
- Owners can attach multiple links to one wishlist item, in an order they choose.
- Gifters see those links rendered as separate clickable elements, not as a comma-soup in a free-text field.
- The `Price` field is gone end-to-end — entity, EF column, DTOs, SPA forms and displays.
- The migration is a single EF migration that does both schema changes together.

**Non-Goals:**
- Per-URL labels (e.g. "Amazon" / "Etsy"). The SPA derives a label from the URL's host. A future change can revisit.
- Storing the resolved title/og:image of each URL. The SPA shows just the host as link text in v1.
- URL deduplication. If an owner adds the same URL twice, we store it twice. Cheap to add later if it becomes annoying.
- Anything in the claim model (reservations, expiry, refresh) — that is a separate change, `expire-stale-reservations`.

## Decisions

### Decision 1: Use a relational child table, not a JSON column

Add a new entity `WishlistItemUrl` mapped to a `WishlistItemUrls` table with `(Id, WishlistItemId, Url, DisplayOrder)`. `WishlistItem` gets a navigation property `IList<WishlistItemUrl> Urls`. EF Core handles the ordered load + cascade-delete with a `HasMany().WithOne().OnDelete(Cascade)` mapping.

**Alternatives considered:**
- *`jsonb` column `Urls text[]` or `jsonb`:* Rejected. EF + Npgsql can map both, but the relational shape gives us free indexing (`(WishlistItemId, DisplayOrder)`), easier per-URL validation, and a more familiar mental model. The list is small (≤10), so there's no perf concern in either direction.
- *Comma-separated string in the existing `Url` column:* Rejected. Lossy, hard to validate per item, ugly in queries.

### Decision 2: Owner-defined order, persisted via `DisplayOrder`

The owner submits URLs as an ordered list (first = primary). We persist `DisplayOrder` as 0-indexed integers in submission order, and EF returns them ordered on read (`HasMany(... ).WithOne(...)` mapping orders via `.OrderBy(u => u.DisplayOrder)` in the query path). On edit, we rebuild `Urls` from the submitted array — simpler than diffing.

**Alternatives considered:**
- *Insertion-order via auto-increment `Id` ordering:* Rejected. Couples the API contract to Id sequencing; reordering would require deleting + re-inserting which loses Ids. Explicit `DisplayOrder` is cheap and self-documenting.
- *Alphabetical / by-domain ordering:* Rejected. The "primary store" is a recipient signal we want to preserve.

### Decision 3: Cap at 10 URLs, enforced in the use case

The use cases (`AddWishlistItemUseCase`, `EditWishlistItemUseCase`) validate `urls?.Count <= 10` and throw `DomainException` on violation — mapping to 400 via the existing `ErrorHandlingMiddleware`. We do not enforce the cap at the DB layer (no check constraint); 10 is a UX/abuse cap, not a data integrity invariant, and a value enforced in two places drifts in two places.

**Alternatives considered:**
- *Db check constraint:* Rejected. Postgres `CHECK` on `count(*)` against a child table requires triggers; not worth the operational surface for a soft cap.
- *Higher cap or no cap:* Rejected. 10 is generous (most items have 1–3 URLs) and protects against accidental paste-everything submissions.

### Decision 4: Per-URL validation = absolute http/https only

We validate each URL with `Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https")`. Relative URLs, `mailto:`, `javascript:`, `data:` are all rejected as 400. Trim surrounding whitespace before validation; empty strings after trim are rejected.

**Alternatives considered:**
- *Allow any URI scheme:* Rejected. Real wishlist URLs are http/https; allowing `javascript:` opens up XSS in the SPA when it renders the clickable link.
- *Validate by attempting to fetch:* Rejected. Adds an outbound HTTP call on every add/edit, fragile, slow, doesn't catch dead links after-the-fact anyway.

### Decision 5: Drop `Price` and the old `Url` column in the same EF migration as the new table

One migration: `CreateTable("WishlistItemUrls", ...)`, `DropColumn("Price", "WishlistItems")`, `DropColumn("Url", "WishlistItems")`. No backfill — confirmed no production data. Rollback is the inverse generated by EF.

**Alternatives considered:**
- *Two migrations (one for table, one for column drops):* Rejected. Same logical change; the down-migration story is cleaner when both halves move together.
- *Soft delete (keep `Price` for a release as nullable, write a follow-up):* Rejected. With no consumers and no data, the deprecation dance buys nothing.

### Decision 6: SPA renders each URL using its host as link text

The wishlist item card shows `Urls` as a small list. Link text = the URL's host (without `www.`); the underlying `href` is the full URL. No favicon fetch, no preview, no label field. Simple, accessible, predictable.

**Alternatives considered:**
- *"Open link" as generic label:* Rejected. With multiple URLs you cannot distinguish them; "amazon.com" vs "etsy.com" is the cheap differentiator.
- *Fetch og:title for each URL on add:* Rejected. Out of scope for this change; would add an outbound HTTP call per URL submission. Tracked as a possible future enhancement.

## Risks / Trade-offs

- **[Trade-off]** The SPA loses any price-based display or sort. We have neither today, so nothing regresses — but it does close the door on price as a sort/filter dimension unless we add it back later.
- **[Trade-off]** Breaking JSON contract: `url` and `price` disappear from `WishlistItemDto`; `urls: string[]` appears. The only client is the in-repo SPA, which is updated in the same PR. Any future external client would need to adapt.
- **[Risk]** The migration drops two columns. If somehow there IS data nobody told us about, it's gone.
  **Mitigation:** confirmed in the change-acceptance discussion that there's no production data. The migration intentionally does not include backfill; reviewing the migration in PR is the last checkpoint.
- **[Risk]** A user pastes a `javascript:` URL hoping the SPA will execute it on click.
  **Mitigation:** the use-case-level scheme allowlist (Decision 4) blocks this before persistence. Even if it slipped through, React's `<a href>` does not execute JS schemes; but defense in depth is cheap.
- **[Risk]** EF migration assumes the existing `Url` and `Price` columns exist. If a future dev re-runs migrations against a hypothetical "fresh" database that already excluded those columns, the migration will fail.
  **Mitigation:** standard EF migration discipline — migrations are forward-only against a known schema. Not a real risk for this codebase, but worth naming.

## Migration Plan

1. Code lands behind a single EF migration that does CREATE TABLE + 2 DROP COLUMNs.
2. Dev workflow: `dotnet ef database update` from `Wishlist.API`; if Postgres is running locally (it is, per `docker compose`), the migration runs in seconds.
3. No data migration is required because there is no production data and the dev database can be reset freely.
4. Rollback: the generated `Down()` method recreates `Price`/`Url` as nullable and drops `WishlistItemUrls`. (Note: any URLs entered while the migration was applied would be lost on rollback, which is acceptable for dev.)

## Open Questions

_None._ All design decisions are settled. Implementation can proceed.
