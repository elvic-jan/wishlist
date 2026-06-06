## Why

A user who forgets their password has no way to recover their account today — the only credentials path is `POST /api/auth/login` against a known password. For an app that holds family-gift data (private wishlists, claims) the lockout cost is real: people will share households, switch devices, and forget passwords on a regular cadence. We need a self-service reset that is safe to expose to the public internet and does not reveal whether an email belongs to a registered user.

## What Changes

- Add a public, rate-limited endpoint `POST /api/auth/forgot-password` that accepts an email and *always* returns 202 Accepted (no enumeration of registered emails). If the email belongs to a real user, the API generates an ASP.NET Identity password-reset token and emails the user a deep link back to the SPA carrying that token.
- Add a public endpoint `POST /api/auth/reset-password` that accepts `{ email, token, newPassword }`, verifies the token via `UserManager.ResetPasswordAsync`, sets the new password, revokes all existing refresh tokens for that user (so other sessions are forced to re-authenticate), and returns 204.
- Add an `IEmailSender` abstraction in `Wishlist.Application` and an SMTP-backed implementation in `Wishlist.Infrastructure` (MailKit-based) so the reset email can be delivered. Configuration drives the SMTP host/port/credentials and the public SPA origin used to build the reset link.
- Add two SPA routes: `/forgot-password` (email form) and `/reset-password` (new-password form that reads `email` and `token` from the query string), each wired to the corresponding API endpoint.
- Apply per-email + per-IP rate limiting on `POST /api/auth/forgot-password` via the built-in ASP.NET Core rate limiter, so an attacker cannot use this endpoint as a spam relay or password-spray oracle.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `user-auth`: add a password-reset flow as a first-class authentication path with enumeration-safe behavior, single-use tokens, refresh-token revocation on successful reset, and rate limiting on the request endpoint. The existing login/refresh/logout requirements are unchanged.

## Impact

- **Affected code**:
  - `src/Wishlist.Application/Interfaces/IEmailSender.cs` (new)
  - `src/Wishlist.Application/UseCases/Auth/RequestPasswordResetUseCase.cs` (new)
  - `src/Wishlist.Application/UseCases/Auth/ResetPasswordUseCase.cs` (new)
  - `src/Wishlist.Application/DTOs/` — `ForgotPasswordRequest`, `ResetPasswordRequest`
  - `src/Wishlist.Application/DependencyInjection.cs` — register the two new use cases
  - `src/Wishlist.Infrastructure/Services/SmtpEmailSender.cs` (new)
  - `src/Wishlist.Infrastructure/Repositories/UserRepository.cs` — add reset-token helpers that delegate to `UserManager`
  - `src/Wishlist.Application/Interfaces/IUserRepository.cs` — surface the new helpers
  - `src/Wishlist.Application/Interfaces/IRefreshTokenRepository.cs` — add a "revoke all tokens for user" helper (if not present)
  - `src/Wishlist.Infrastructure/DependencyInjection.cs` — register `IEmailSender`, bind `SmtpOptions` from configuration
  - `src/Wishlist.API/Controllers/AuthController.cs` — two new actions plus rate-limit attributes
  - `src/Wishlist.API/Program.cs` — `AddRateLimiter(...)` configuration + `UseRateLimiter()`
  - `src/Wishlist.API/appsettings.json` + `appsettings.Development.json` — `Smtp` and `App:PublicBaseUrl` sections
  - `src/Wishlist.Web/src/pages/ForgotPassword.tsx` (new)
  - `src/Wishlist.Web/src/pages/ResetPassword.tsx` (new)
  - `src/Wishlist.Web/src/routes/index.tsx` (or equivalent) — wire the two routes
  - `src/Wishlist.Web/src/api/auth.ts` — `requestPasswordReset` / `resetPassword` client methods
- **APIs**: two new public endpoints under `/api/auth`. No change to existing login/refresh/logout/me contracts.
- **Dependencies**: add `MailKit` (or `MimeKit` + `MailKit`) to `Wishlist.Infrastructure`. No new SPA packages.
- **Operational**: production needs SMTP credentials (or a transactional-email provider that speaks SMTP) and a verified sender domain; dev uses a local SMTP catcher (recommended: smtp4dev or MailHog) so we never accidentally email real users from a dev machine.
- **Security**: introduces a new attack surface (token-bearing public endpoint, email-spam vector). Mitigations live in design.md.
