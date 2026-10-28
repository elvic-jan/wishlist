## 1. Application and infrastructure

- [x] 1.1 `IGroupRepository.GetCircleMatesAsync(userId)`: one row per (person, shared circle), caller excluded; implement in `GroupRepository` as one join
- [x] 1.2 `ConnectedPersonDto` / `SharedCircleDto` and `ListConnectedPeopleUseCase`: group by person, sort people by name (case-insensitive, then id) and circles by name; register in DI

## 2. API

- [x] 2.1 `GET /api/wishlists/people` in `WishlistsController`

## 3. Tests

- [x] 3.1 In memory: each person once with their shared circles, caller and strangers left out, ordering, empty list
- [x] 3.2 Endpoint: 401 without a session
- [x] 3.3 Postgres: the query returns only shared circles, no duplicates, not the caller

## 4. Web

- [x] 4.1 `WishlistsPage` at `/wishlists`: My wishlist row, people list with circle badges, filter (name or circle, ignoring case and accents), loading placeholders, empty and no-match messages
- [x] 4.2 Bounded badges: width cap with ellipsis and tooltip, at most 5 plus "+N more", matching circles first while filtering
- [x] 4.3 `AppShell`: "Wishlists" instead of "My wishlist", highlighted on `/wishlists` and `/wishlist/*`
- [x] 4.4 `MemberWishlistPage`: title "Name's wishlist" from the people list, "Wishlist" as fallback
- [x] 4.5 Check on phone width and in dark mode, with long circle names and many circles
