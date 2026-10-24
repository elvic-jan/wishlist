## Why

When the site went public on Azure, anyone who found the URL could create accounts, which opens the door to spam sign-ups, abuse, and unwanted cost. The site was taken offline (ingress disabled) on 2026-10-10 and must stay offline until registration is restricted to people an existing member has invited.

## What Changes

- **BREAKING**: `POST /api/auth/register` requires a valid, unexpired group invite code. Requests without one (or with an unknown/expired code) are rejected and no account is created.
- Registering with an invite code also joins the new user to the invite's group in the same request, so a single link takes a newcomer from "no account" to "member of the family group".
- First-account bootstrap: while the database has **zero** users, registration without an invite code is allowed so the owner of a fresh deployment can create the first account (and then a group and invites). The moment one user exists, this path closes automatically.
- Invite-code guessing protection: after too many wrong invite codes from one client IP, every invite-code attempt from that IP (register or join) is refused with 429 Too Many Requests for a cool-down window, even if the code is correct. Malformed codes are rejected without a database lookup and count as wrong guesses. Lockouts are logged.
- New public endpoint `GET /api/auth/registration` reports whether an invite is required, so the SPA can show the right screen.
- SPA:
  - `/join/:code` for a signed-out visitor offers **Create account** (in addition to Sign in), which carries the code to the register page.
  - `/register` reads the invite code from the query string; without one (and outside bootstrap) it shows an "invitation only" message instead of the form.
  - The login page no longer advertises open registration; it explains that accounts come from an invite link.
- Existing invitation semantics are unchanged: links remain valid for 7 days and may be used by several people within that window.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `user-auth`: "User registration" becomes invite-only, with a first-user bootstrap exception and a registration-status endpoint.
- `groups`: "Join group via invitation" gains the scenario of joining as part of registering a new account, and a new requirement rate-limits invite-code guessing on both the register and join paths.

## Impact

- **API**: `AuthController.Register` contract changes (`inviteCode` added to `RegisterRequest`); new `GET /api/auth/registration`. Clients that register without a code break by design.
- **Application**: `RegisterUserUseCase` depends on `IInvitationRepository` and `IGroupRepository`; `IUserRepository` gains a "any users exist" query.
- **Infrastructure**: new in-memory `InviteCodeAttemptLimiter` (singleton), modelled on the existing `ForgotPasswordRateLimiter`.
- **Web**: `RegisterPage`, `JoinGroupPage`, `LoginPage`, `AuthContext.register`.
- **Tests**: new API tests for invite-required, invalid/expired code, bootstrap open/closed, and registration-joins-group.
- **Ops**: once merged and deployed, the site can be brought back online (re-run `infra` to re-enable ingress, then deploy). The production database already holds the owner's account, so the bootstrap path is closed there from the start.
- No database migration is needed.
