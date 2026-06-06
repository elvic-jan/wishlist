## Context

The wishlist API already authenticates with email + password (`POST /api/auth/login`) and issues a JWT access token plus an HttpOnly + Secure + SameSite=Strict refresh-token cookie. ASP.NET Core Identity is wired up (`AddIdentityCore<ApplicationUser>().AddDefaultTokenProviders()`), and `UserManager<ApplicationUser>` is already used by `UserRepository`. There is no email infrastructure: nothing in `Wishlist.Infrastructure` sends mail today.

Adding a forgot/reset flow safely is not just "two endpoints" — the request endpoint is unauthenticated, free to call, and operates on email addresses, so it doubles as a potential user-enumeration oracle and an email-spam vector. The token-redemption endpoint also accepts a credential that, if leaked or guessed, replaces a real password, so it has to be at least as strict as login.

## Goals / Non-Goals

**Goals:**
- A user who knows their email can recover access without admin intervention.
- The forgot-password endpoint MUST NOT reveal whether a given email is registered.
- A reset token MUST be single-use and time-limited; after successful reset, all of the user's existing refresh tokens are revoked so other sessions are kicked.
- The flow works through the SPA over HTTPS (matches the invariant set by the previous change `switch-config-to-https`).
- Email-sending is abstracted behind an interface so production can swap SMTP for a transactional-email provider without touching application code.

**Non-Goals:**
- SMS-based reset, social-login-based recovery, or admin-driven password reset (not requested).
- Account recovery for a user who has lost access to their email inbox. If the email is gone, the user has to contact support; this is out of scope.
- Email-template theming, i18n, or rich HTML emails. v1 sends a clean plain-text email (with a single link) plus a minimal HTML body. Future change can move to templates.
- Changing the existing password policy (length, complexity). The reset endpoint applies the same policy that registration uses, via Identity's configured options.

## Decisions

### Decision 1: Use ASP.NET Identity's built-in password-reset token

Use `UserManager.GeneratePasswordResetTokenAsync(user)` and `UserManager.ResetPasswordAsync(user, token, newPassword)`. The token is a signed, data-protected payload (no separate DB table), automatically scoped to the user and purpose, and respects `DataProtectionTokenProviderOptions.TokenLifespan` for expiry.

Set `DataProtectionTokenProviderOptions.TokenLifespan = TimeSpan.FromMinutes(30)` so reset links expire after 30 minutes.

**Alternatives considered:**
- *Roll our own token table (`PasswordResetTokens`) with a per-user hash + expiry column:* Rejected — extra surface area, our own crypto choices, and another migration, for no behavioral gain over Identity's provider. Revisit if we later need server-side per-token revocation visibility (e.g. an admin "invalidate outstanding resets" tool).
- *JWT as the reset token:* Rejected — JWTs are stateless and would force us to either accept any non-expired token (no single-use guarantee) or add a revocation table, which defeats the simplicity. Identity's data-protected token is the right primitive here.

### Decision 2: Enumeration-safe `POST /api/auth/forgot-password`

The endpoint ALWAYS returns 202 Accepted with an empty body, whether the email matches a registered user or not. The branch that finds a user enqueues the email send; the branch that does not, silently no-ops. Timing is also normalized: both branches perform a constant-time-ish amount of work (we do at least a `UserManager.FindByEmailAsync` and an artificial small delay for the "not found" branch) so the response time doesn't leak the existence of an account.

**Alternatives considered:**
- *Return 404 when the email is unknown:* Rejected — turns the endpoint into a public user-enumeration oracle. Standard anti-pattern for any password-reset implementation.
- *Return a generic 200 with a "if your email matches..." message body in JSON:* Same outcome; 202 with empty body is slightly cleaner and conventional for "we've enqueued something."

### Decision 3: Per-email + per-IP rate limit on `POST /api/auth/forgot-password`

Use ASP.NET Core's built-in `AddRateLimiter` with a partitioned `FixedWindowLimiter`. Two partitions: one keyed on the normalized email submitted, one on the client IP. Each partition allows a small number of requests per window — start with **5 requests per 15 minutes per email** and **20 requests per 15 minutes per IP**, both returning 429 when exceeded.

This caps the email-spam vector (attacker can't request resets for `victim@example.com` more than 5 times in a window) and the password-spray oracle (attacker can't enumerate 10,000 emails from a single IP in a window). We accept that a distributed attacker can still send up to 5 emails to any one address per window per source IP; that is an inherent trade-off, mitigated further by the no-reveal response.

**Alternatives considered:**
- *Per-user rate limit on `UserManager`:* Doesn't help — the rate limit must apply *before* we know whether the email is real, otherwise we leak existence by 429-vs-202.
- *No rate limit, rely on Cloudflare/upstream:* Rejected — we don't ship with a CDN and we shouldn't assume one. App-layer rate limiting is cheap and correct.

### Decision 4: Revoke all refresh tokens on successful reset

When `UserManager.ResetPasswordAsync` succeeds, the use case calls `IRefreshTokenRepository.RevokeAllForUserAsync(userId)` (new method) in the same unit-of-work. Rationale: a user who resets their password is implicitly saying "someone may have my old credentials." Existing sessions must be kicked.

The endpoint does NOT issue new tokens. The user goes back to the login page, signs in with the new password, and gets a fresh access token + refresh cookie there. This keeps the reset endpoint single-purpose and avoids the awkward UX of "you reset your password and were also just logged in on a device that was never authenticated."

**Alternatives considered:**
- *Leave existing refresh tokens valid:* Rejected — defeats the security purpose of a reset.
- *Issue a fresh JWT + refresh cookie inline:* Rejected — see UX argument above. Also, the reset endpoint is hit from a public link in an email, and we don't want to start a session in whatever browser happens to click that link without an explicit login.

### Decision 5: Send a "your password was changed" notification email after a successful reset

After `ResetPasswordUseCase` succeeds and revokes the user's refresh tokens, the use case dispatches a second email to the same address with subject "Your Wishlist password was changed" and a body that explains the change happened, includes the approximate timestamp (UTC), and tells the user what to do if they did not initiate it (contact support, re-request a reset). The email is fire-and-forget on the same `IEmailSender` and uses the same 5-second timeout pattern as the request-side email; a delivery failure here MUST NOT prevent the 204 response — the password change has already happened and the user can still log in.

**Why it's worth doing:** if an attacker compromises the inbox and uses the reset link to take over the account, the legitimate owner — who still controls the email — gets an immediate "this just happened" notification with an actionable next step. This is the only out-of-band signal the user gets after their password changes; it pairs naturally with refresh-token revocation (Decision 4) to bound the damage of a stolen reset link.

**Alternatives considered:**
- *Skip the notification (rely on the user noticing on next login):* Rejected — by the time the user notices, the attacker has already changed the password to something the legitimate owner doesn't know, and recovery becomes a support ticket. The notification gives the user a chance to react inside the same 30-minute window.
- *Send a richer email containing a "revert" or "lock account" link:* Rejected for v1 — that would require us to keep a copy of the previous password hash (revert) or build a self-service lockout flow (lock), both of which are larger features. v1 notification is informational only; the recovery path is "request a new reset" or "contact support."

### Decision 6: SMTP via MailKit, configured per environment, with a dev-safe default

Add MailKit (`MailKit` package) to `Wishlist.Infrastructure` and implement `IEmailSender` as `SmtpEmailSender` reading `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:UseStartTls`, `Smtp:FromAddress`, `Smtp:FromName` from configuration.

Development defaults (`appsettings.Development.json`) point at `localhost:1025` (smtp4dev / MailHog convention) with no credentials. This ensures a fresh dev machine never accidentally emails the public internet — at worst, the send fails because no local SMTP catcher is running. Production overrides via environment variables / user secrets.

The SPA reset link uses `App:PublicBaseUrl` from configuration so dev points at `https://localhost:5173` and prod at the production origin.

**Alternatives considered:**
- *Use a transactional API client (SendGrid SDK, Postmark, Resend):* Rejected for v1 — the `IEmailSender` interface keeps the door open; we don't want a vendor binding before we have a deployment story.
- *Send mail synchronously from the controller using the host's outbound network:* That is what we do (no queue). For v1 the cost/benefit of adding a real outbox + background worker is not worth it. Document this as a known limitation in Risks below.

## Risks / Trade-offs

- **[Risk]** SMTP send happens synchronously inside the request. If the SMTP server is slow or unreachable, the forgot-password endpoint hangs (we still return 202 though, because we await with a short timeout).
  **Mitigation:** wrap the send in `Task.WhenAny` with a 5-second timeout; on timeout, log a warning and still return 202 (the user retries; rate limit covers abuse). Document an outbox/background-worker as a follow-up change if this becomes a real ops issue.
- **[Risk]** A user with a real account whose email provider silently drops the reset email gets stuck in a loop ("I requested a reset and nothing arrived").
  **Mitigation:** log every send attempt with the recipient address and the SMTP server's response code at the SMTP layer (NOT in API access logs, to avoid leaking enumeration via log scraping). Support can correlate by user-reported time + email.
- **[Risk]** A reset link in an inbox is a bearer credential. Anyone with read access to the inbox can take over the account within the 30-minute window.
  **Mitigation:** 30-minute lifespan + single-use (Identity's token provider invalidates the token on use) + refresh-token revocation on success. We do not add IP-binding or device-binding to the link — that breaks the common case where the user requests on phone and clicks on laptop.
- **[Risk]** Configuration drift between dev defaults and prod: a misconfigured prod environment could fall back to `localhost:1025` and silently fail to send any reset emails.
  **Mitigation:** in `SmtpEmailSender`, treat a missing `Smtp:Host` as a fatal startup error in non-Development environments. Fail fast at boot rather than swallowing sends at runtime.
- **[Trade-off]** The reset endpoint does not log the user in. Slightly more friction (one extra page navigation) but materially cleaner threat model (see Decision 4).

## Migration Plan

No data migration required — Identity's reset token is stateless. The SMTP options and `App:PublicBaseUrl` need to be set in prod before deploying; if they are missing, the API fails fast on startup (per the Decision-5 risk mitigation), so a misconfigured deploy is loud rather than silently broken.

Rollback is straightforward: revert the code change. No persisted state is created by this feature beyond the (revoked) refresh tokens, and those are already revocable through existing mechanisms.

## Open Questions

- Should we expose a `GET /api/auth/reset-password?email=...&token=...` "is this token still valid?" probe so the SPA can show a friendly "your link expired" page before the user types a new password? Probably yes for UX, but it leaks token-validity timing. Proposed default: skip in v1; the SPA shows the same error after submit.
