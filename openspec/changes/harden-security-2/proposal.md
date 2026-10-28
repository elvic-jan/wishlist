## Why

The security review of `main` (2026-10-28) left findings after the first batch (`harden-security`). This change fixes the ones that live in the code:

1. **Refresh tokens are stored as plain text.** Anyone who can read the `RefreshTokens` table (a backup, a log of a query, a leaked connection string) can sign in as any user with an active session. Rotation also isn't atomic: two requests with the same token at the same moment both get a fresh one. Revoked and expired rows are never removed.
2. **No length limits on text input.** An overlong circle name or item title gives a 500 (the column is too short); display names and descriptions have no limit at all, so one request can store megabytes that then show up in lists and emails.
3. **The placeholder JWT key isn't refused.** `appsettings.json` ships `change-this-to-a-long-random-secret-in-production`. If the platform secret were ever missing, production would sign tokens with a key that's public on GitHub.
4. **Invite links use the request's Host header.** A request with a forged Host makes the API return an invite link to another site.
5. **Sign-in timing reveals which emails have an account.** An unknown email answers at once; a known one first hashes the password, which takes noticeably longer.

## What Changes

- **Refresh tokens are stored as SHA-256 hashes.** The cookie keeps the random token; the database only has its hash. A migration hashes the existing rows, so nobody is signed out.
- **Refresh is atomic:** the old token is revoked by one conditional update, and only the request that revoked it gets a new token. The SPA serializes refreshes across tabs (Web Locks) and retries once, so two tabs opening at once don't sign each other out.
- **Old refresh tokens are cleaned up:** whenever a user gets a new token, their expired tokens and tokens revoked more than a day ago are deleted.
- **Text input has limits**, checked before anything is stored, with a 400 and a clear message: display name 100, email 256, password 256, circle name 200, item title 300, item description 5000, URL 2048 characters. The SPA's inputs get the same `maxLength`. Updating the display name also refuses an empty name.
- **Startup refuses a weak JWT key** outside Development: the placeholder value, or a key shorter than 32 bytes.
- **Invite links are built from `App:PublicBaseUrl`**, like every other link the app sends.
- **Password sign-in takes the same time for unknown emails**: the password is checked against a dummy hash.
- Out of scope (needs Azure changes, separate step): a least-privilege database login for the app and a narrower database firewall. The code-quality findings stay in the backlog.

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `user-auth`: refresh tokens hashed, rotated atomically and cleaned up; sign-in timing; length limits on names, emails and passwords.
- `groups`: invite links use the configured address; circle name limit.
- `wishlists`: title, description and URL limits.
- `hosting`: startup refuses a weak JWT key.

## Impact

- **Domain/Infrastructure**: `RefreshToken.Token` becomes `TokenHash`; `TokenService` hashes, consumes atomically and cleans up; migration `HashRefreshTokens` (renames the column and hashes existing values in SQL). `UserRepository.CheckPasswordAsync` verifies against a dummy hash when there's no user.
- **Application**: `InputLimits`; checks in registration, passkey sign-up, profile update, circle creation, item add/edit, URL validator; `GenerateGroupInvitationUseCase` uses `IAppUrlProvider`; `ITokenService.ConsumeRefreshTokenAsync`.
- **API**: JWT key check at startup; profile update goes through a use case.
- **Web**: cross-tab refresh lock and one retry; `maxLength` on inputs.
- **Tests**: limits, invite link host, JWT key check, sign-in for unknown emails, Postgres tests for hashing, atomic rotation and cleanup.
