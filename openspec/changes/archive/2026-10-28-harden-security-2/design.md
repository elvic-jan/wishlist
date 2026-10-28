## Context

Refresh tokens are 64 random bytes (base64) in an HttpOnly cookie, stored as-is in `RefreshTokens.Token` (unique index) and rotated on every refresh: `RefreshTokenUseCase` reads the token, then revokes it in a separate save. The SPA (`api/client.ts`) shares one in-flight refresh per tab; tabs don't coordinate.

Column limits exist for circle names (200), item titles (300) and URLs (2048); display names and descriptions are `text`. Use cases check only for empty values. `DomainException` maps to 400.

## Decisions

**1. Store `SHA-256(token)` as lowercase hex, rename the column to `TokenHash`.**
The token is 512 bits of randomness, so a fast hash is enough (no salt or slow hash needed: there's nothing to guess). The migration renames the column and runs `UPDATE "RefreshTokens" SET "TokenHash" = encode(sha256(convert_to("TokenHash", 'UTF8')), 'hex')`, so existing cookies keep working. The unique index stays. *Alternative:* sign everyone out. Not needed and annoying on phones.

**2. Rotation consumes the token with one conditional update.**
`ITokenService.ConsumeRefreshTokenAsync(token)` runs `UPDATE … SET RevokedAt = now WHERE TokenHash = h AND RevokedAt IS NULL AND ExpiresAt > now` (EF `ExecuteUpdateAsync`) and returns the user id only when it changed a row. Two concurrent refreshes: one wins, the other gets 401. `GetActiveRefreshTokenAsync` is no longer used by refresh.

**3. The SPA serializes refreshes across tabs and retries once.**
With atomic rotation, two tabs restoring a session at the same time would make one of them lose. `refreshAccessToken` runs inside `navigator.locks.request('giftcircles-refresh', …)` when available, so the second tab waits and then refreshes with the cookie the first tab just received. If a refresh still fails, it tries once more after a short pause before going to the login page. *Alternative:* a grace period on the server where a just-revoked token still works. More state and weaker rotation.

**4. Cleanup happens when a token is created.**
`CreateRefreshTokenAsync` deletes the user's rows that expired, or were revoked more than a day ago (`ExecuteDeleteAsync`). No job; the cost is one delete per sign-in or refresh, using a new index on `UserId`.

**5. `InputLimits` in Application, checked in the use cases.**
One static class with the numbers and a `Check(value, max, label)` helper throwing `DomainException("<label> can be at most N characters.")`. Values match the columns where they exist; display name 100 and description 5000 are new and app-only (no schema change, so existing longer data stays readable; editing it requires shortening). The SPA sets the same `maxLength` so people can't type past it.

**6. JWT key check at startup, outside Development.**
In `Program.cs` before building: refuse when `Jwt:Key` is empty, equals the placeholder, or is under 32 bytes UTF-8. HS256 already fails on short keys at the first sign-in; this moves the failure to startup, where the platform keeps the previous revision.

**7. Dummy password hash for unknown users.**
`UserRepository.CheckPasswordAsync` with no user verifies the password against a fixed hash made once with the same `IPasswordHasher`, then returns false. `LoginUserUseCase` calls it for unknown emails too.

## Risks / Trade-offs

- [Migration hashes tokens in SQL] → `sha256()` exists since Postgres 11; production runs a newer version. A Postgres test migrates to the previous migration, stores a plain token, migrates on and refreshes with that token.
- [A browser without Web Locks (very old)] → falls back to the retry.
- [Existing data longer than the new limits] → shown as is; only a change to that field is refused.
