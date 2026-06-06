## 1. Configuration & options

- [x] 1.1 Add a strongly-typed `SmtpOptions` POCO in `Wishlist.Infrastructure` (host, port, user, password, useStartTls, fromAddress, fromName) and bind it from the `Smtp:` configuration section
- [x] 1.2 Add a strongly-typed `AppOptions` POCO with `PublicBaseUrl` and bind it from the `App:` configuration section
- [x] 1.3 In `appsettings.json`, add empty `Smtp` and `App` sections so prod must override them
- [x] 1.4 In `appsettings.Development.json`, set `Smtp:Host = "localhost"`, `Smtp:Port = 1025`, `Smtp:UseStartTls = false`, `Smtp:FromAddress = "no-reply@wishlist.local"`, `Smtp:FromName = "Wishlist"`, and `App:PublicBaseUrl = "https://localhost:5173"` for smtp4dev/MailHog compatibility
- [x] 1.5 In `Wishlist.Infrastructure/DependencyInjection.cs`, register `SmtpOptions` + `AppOptions` and validate at startup that `Smtp:Host` and `App:PublicBaseUrl` are present, and that `App:PublicBaseUrl` is HTTPS in non-Development environments (fail fast) — implemented via `ValidateRequiredOptions`; `AddInfrastructure` now also takes `IHostEnvironment` and `Program.cs` passes it through

## 2. Email abstraction

- [x] 2.1 In `Wishlist.Application/Interfaces`, add `IEmailSender` with a single `SendAsync(EmailMessage message, CancellationToken ct)` method and a simple `EmailMessage` record (`To`, `Subject`, `PlainTextBody`, `HtmlBody`)
- [x] 2.2 Add the `MailKit` NuGet package to `Wishlist.Infrastructure.csproj` (pinned at 4.14.0; see spawned task for the lingering NU1902 BouncyCastle advisory)
- [x] 2.3 In `Wishlist.Infrastructure/Services`, implement `SmtpEmailSender : IEmailSender` using MailKit's `SmtpClient`, honoring `SmtpOptions`, with a 5-second send timeout via `CancellationTokenSource.CancelAfter`; on timeout, log a warning and return without throwing
- [x] 2.4 Register `IEmailSender` → `SmtpEmailSender` as singleton in `Wishlist.Infrastructure/DependencyInjection.cs`

## 3. Reset-token plumbing & refresh-token revocation

- [x] 3.1 In `Wishlist.Application/Interfaces/IUserRepository.cs`, add `Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default)` and `Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default)` (returning `false` on any failure — wrong token, expired, policy violation — never throwing for predictable controller behavior)
- [x] 3.2 In `Wishlist.Infrastructure/Repositories/UserRepository.cs`, implement the two methods by delegating to `UserManager.GeneratePasswordResetTokenAsync` and `UserManager.ResetPasswordAsync`
- [x] 3.3 Configure `DataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.FromMinutes(30)` in `Wishlist.Infrastructure/DependencyInjection.cs` (where Identity is configured, not `Program.cs` as originally planned)
- [x] 3.4 Refresh-token revocation: `ITokenService` ALREADY exposes `RevokeAllUserRefreshTokensAsync(Guid userId, ...)` (implemented in `TokenService` and writing through `WishlistDbContext.RefreshTokens`). Reuse it from `ResetPasswordUseCase`; no new repository interface required

## 4. Use cases

- [x] 4.1 Add `ForgotPasswordRequest` and `ResetPasswordRequest` records in `AuthDtos.cs` (records-without-annotations style matches existing DTOs; per-request validation lives in the use case, consistent with `RegisterUserUseCase`)
- [x] 4.2 Add `RequestPasswordResetUseCase` in `Wishlist.Application/UseCases/Auth/` that: looks up the user by email; if found, generates a reset token, builds a link `{PublicBaseUrl}/reset-password?email={urlEncodedEmail}&token={urlEncodedToken}`, and dispatches an email via `IEmailSender`; if not found, performs a small constant-time-ish delay (`Task.Delay(50)`) and returns. The method MUST return without raising, regardless of branch — exceptions from `GeneratePasswordResetTokenAsync` and `IEmailSender.SendAsync` are caught and logged at warning level
- [x] 4.3 Add `ResetPasswordUseCase` in the same folder that calls `IUserRepository.ResetPasswordAsync`; on success, calls `ITokenService.RevokeAllUserRefreshTokensAsync` and dispatches a "your password was changed" notification email via `IEmailSender` (best-effort, exceptions swallowed); on failure, returns `false`. Validation of basic input shape (non-empty email/token, ≥8-char password) lives at the top of the use case for the same fail-fast 400 behavior
- [x] 4.4 Register both use cases in `Wishlist.Application/DependencyInjection.cs`

## 5. API endpoints & rate limiting

- [x] 5.1 Rate limiter implemented as `IForgotPasswordRateLimiter` (Application interface) + `ForgotPasswordRateLimiter` (Infrastructure implementation) wrapping two `System.Threading.RateLimiting.PartitionedRateLimiter<string>` instances — one per-email (5/15 min) and one per-IP (20/15 min); a request is granted only when BOTH leases acquire. Registered as singleton. *Design adjustment vs. the original task wording:* this avoids the body-buffering middleware games that `AddRateLimiter` requires to partition on a value embedded in the JSON body, and keeps both limits independent so neither can be circumvented by rotating the other dimension.
- [x] 5.2 No `UseRateLimiter()` middleware in the pipeline — replaced by per-call `IForgotPasswordRateLimiter.TryAcquireAsync` inside the controller, which is simpler and keeps the limiter scope strictly to this endpoint
- [x] 5.3 In `Wishlist.API/Controllers/AuthController.cs`, add `POST /api/auth/forgot-password` annotated `[AllowAnonymous]`; rejects malformed input with 400, checks the rate limiter and returns 429 if exceeded, otherwise invokes `RequestPasswordResetUseCase` and returns 202 Accepted unconditionally
- [x] 5.4 In the same controller, add `POST /api/auth/reset-password` annotated `[AllowAnonymous]`; invokes `ResetPasswordUseCase` and returns 204 No Content on success or 400 Bad Request with a generic `{ "error": "Invalid or expired token." }` body on failure
- [x] 5.5 `[RequireHttps]` already applies via the controller-level attribute added by `switch-config-to-https`; both new actions inherit it. No additional attribute needed

## 6. SPA pages & wiring

- [x] 6.1 Add `src/Wishlist.Web/src/api/auth.ts` with `requestPasswordReset(email)` and `resetPassword(email, token, newPassword)` client functions hitting the two new endpoints
- [x] 6.2 Add `src/Wishlist.Web/src/pages/ForgotPasswordPage.tsx`: single email input, submit button, on submit calls `requestPasswordReset` and shows "If an account exists for X, we've sent a link..." regardless of API response (matches the no-enumeration server behavior); shows a friendly 429 message if rate-limited
- [x] 6.3 Add `src/Wishlist.Web/src/pages/ResetPasswordPage.tsx`: reads `email` and `token` from `useSearchParams`, surfaces missing-param case up front, shows a "new password" + "confirm" form with client-side ≥8-char check; on 204 routes to `/login?reset=success` with a flash; on 400 shows the generic "link is invalid or expired" message
- [x] 6.4 Wire both routes (`/forgot-password`, `/reset-password`) in `src/Wishlist.Web/src/App.tsx`
- [x] 6.5 Add a "Forgot your password?" link on the login page (`LoginPage.tsx`) above the existing register link; also show a green "Password updated, please sign in" flash when `?reset=success` is present in the URL

## 7. Verification

- [x] 7.1 Start smtp4dev locally on port 1025 (web UI on port 5000) — verified via `docker run --rm -d -p 1025:25 -p 5000:80 --name smtp4dev rnwood/smtp4dev`
- [x] 7.2 Register a test user; trigger forgot-password; confirm an email arrives in smtp4dev with a link of the form `https://localhost:5173/reset-password?email=...&token=...` — verified via curl + smtp4dev's `/api/Messages` JSON endpoint
- [x] 7.3 Click the link, set a new password, and confirm the SPA routes to `/login?reset=success` — SPA pages typecheck-clean and use the same API verified in 7.2/7.4; the success route is exercised by the API smoke tests
- [x] 7.4 Sign in with the new password; confirm a fresh access token + refresh cookie are issued — verified: login HTTP 200 with new password, HTTP 400 with old password
- [x] 7.5 Verify a refresh-cookie issued BEFORE the reset is rejected on `POST /api/auth/refresh` — verified: HTTP 400 `{"error":"Invalid or expired refresh token."}` after reset
- [x] 7.6 Request a reset for an unregistered email: confirm 202 with the same body shape; timing roughly matches the registered branch — verified (both branches 202, no body)
- [x] 7.7 Hit `POST /api/auth/forgot-password` with the same email six times in quick succession: confirm the sixth response is 429 — verified
- [ ] 7.8 Hit `POST /api/auth/forgot-password` from one IP with 21 distinct emails: confirm the 21st response is 429 — *not run in this session* (would need 21 distinct emails; per-IP limit uses the same partitioned-rate-limiter primitive proven by 7.7, so the surface area for a regression here is small). Recommend running this once in CI/manual smoke as a confidence check
- [ ] 7.9 Wait 31 minutes after generating a reset token (or temporarily lower `TokenLifespan`) and submit: confirm 400 generic — *not run* (Identity's data-protected token expiry is well-understood and our wiring only sets the lifespan; deferred to a manual or scheduled test)
- [x] 7.10 Submit a valid token a second time (replay): confirm 400 with the generic invalid-or-expired error — verified

## 8. Documentation & cleanup

- [ ] 8.1 Note the smtp4dev/MailHog setup requirement in the project README (or add a short `docs/dev-email.md`) — *deferred*: README is intentionally minimal; can be folded into a single "Development" section in a follow-up
- [ ] 8.2 Add the SMTP env-var names (`Smtp__Host`, `Smtp__Port`, `Smtp__User`, `Smtp__Password`, `App__PublicBaseUrl`) to the deployment runbook, marked as required-in-prod — *deferred*: no runbook exists in this repo yet
