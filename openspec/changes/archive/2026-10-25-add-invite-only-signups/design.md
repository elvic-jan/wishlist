## Context

Registration is currently open: `POST /api/auth/register` (`RegisterUserUseCase`) accepts any email, display name, and password. Group invitations already exist. A member generates a link (`/join/:code`, a 32-hex-char GUID code, valid 7 days), and a signed-in user redeems it through `POST /api/invitations/{code}/join` (`JoinGroupViaInvitationUseCase`). Invitations can be reused within their window: `UsedAt` is stamped but never checked, so one link can be shared with a whole family.

A signed-out visitor who opens `/join/:code` today only gets a **Sign in** button, so a newcomer has to find `/register` on their own and then come back to the link.

Production state: the owner's account already exists in the Azure Postgres database. The site is offline (ingress disabled) until this change ships.

Users, groups, and invitations share the same `WishlistDbContext` (an `IdentityDbContext`). Domain exceptions are mapped to HTTP status codes by the API exception middleware (`NotFound→404`, `Forbidden→403`, `Gone→410`, `Conflict→409`, `Domain→400`).

## Goals / Non-Goals

**Goals:**
- No account can be created without a valid, unexpired invite code, except for the very first account on an empty database.
- One link, one flow: a newcomer who opens an invite link can create an account and join the group in a single step.
- Reuse the existing invitation model unchanged. No schema migration.

**Non-Goals:**
- Single-use or per-email invitations, invite revocation, or an admin UI. These can come later if needed.
- CAPTCHA, or a general request-rate limit on registration. Invite-code guessing is limited (Decision 6); beyond that, the remaining abuse surface is a leaked invite link, which expires in 7 days.
- A global (all-IP) lockout. It would let one attacker lock every real invitee out.
- Email verification of new accounts.
- Changes to login, refresh, logout, or password reset.

## Decisions

### 1. Invite code travels in the register request body

`RegisterRequest` gains an optional `InviteCode`. `RegisterUserUseCase` validates it before creating the user.

The checks run in this order:
1. Input validation (400).
2. Invite lookup, which also decides whether a code is required at all.
3. Duplicate email check (409).

This ordering means an uninvited caller never learns whether an email is registered: without a valid code they always get 403/404/410 before the email is looked at.

| Situation | Result |
|---|---|
| Users exist, no code | 403 `Registration is by invitation only.` |
| Unknown code | 404 (same as join) |
| Expired code | 410 (same as join) |
| Valid code | Create user, add as `Member` of the invite's group, stamp `UsedAt`, issue tokens |

*Alternative considered:* a separate `POST /api/invitations/{code}/register` endpoint. Rejected because it would duplicate the token and cookie handling in `AuthController`, and register would still need a guard anyway.

### 2. Account creation and group membership are atomic

Creating the user and adding the membership happen in one database transaction. Without this, a failure between the two steps would leave an orphaned account that belongs to no group and can't see anything.

A small Application-layer port, `IUnitOfWork.ExecuteInTransactionAsync(Func<CancellationToken, Task>)`, is implemented in Infrastructure with `db.Database.BeginTransactionAsync` (via the execution strategy, so it stays compatible with Npgsql retries). This keeps `RegisterUserUseCase` free of EF Core types.

*Alternative considered:* call the existing `JoinGroupViaInvitationUseCase` after creating the user. Simpler, but not atomic, and it would re-fetch and re-validate the invite.

### 3. First-user bootstrap: open only while there are zero users

`IUserRepository.AnyAsync()` is added. If it returns false, registration without a code is allowed and creates a plain account with no group. The owner then creates a group and invites.

- This closes itself and needs no secret, config flag, or manual step to switch off.
- Production already has a user, so the bootstrap path is closed there from day one.

*Alternatives considered:*
- A `Registration:BootstrapEmail` config allowlist. More explicit, but it's one more setting to manage, and it gives no extra protection on a database that already has users.
- A seeded admin account. Rejected because it would put a default password in config.

### 4. `GET /api/auth/registration` tells the SPA what to render

This is an anonymous endpoint that returns `{ "inviteRequired": bool }`, which is `true` whenever any user exists. The SPA calls it only on `/register` when no invite code is present.

The endpoint leaks nothing useful: after bootstrap it always returns `true`.

### 5. SPA flow

- **Check the code first.** A new anonymous `GET /api/invitations/{code}` returns 200 (with `expiresAt`), 404 or 410 and doesn't mark the invite as used. Both invite pages call it on load, so someone with a dead link is told straight away, instead of only after filling in the whole form. Wrong codes here use the same guess budget (Decision 6).
- **`/join/:code`, signed out:** if the check fails, show why the invitation can't be used. Otherwise show two actions:
  - **Create account** links to `/register?invite=<code>`.
  - **Sign in** keeps the current behavior, `/login?redirect=/join/<code>`.
- **`/register?invite=<code>`:** the same check first (opening this URL directly is possible too); then the normal form, which sends `inviteCode`. On success, navigate to `/groups/<groupId>`, where `groupId` comes from a new optional `joinedGroupId` field in the register response. The page also shows a short "You've been invited to join a group" line.
- **`/register` without a code:** call `GET /api/auth/registration`. If an invite is required, show an "invitation only" card with a link to sign in. If not (bootstrap), show the form.
- **Login page:** replace "Don't have an account? Register" with "New here? Ask a family member for an invite link."
- **404/410 from register:** show the server message ("Invitation not found." / "This invitation has expired.") and suggest asking for a new link.

### 6. Rate-limit wrong invite-code guesses per client IP

Invite codes are `Guid.NewGuid()` in `"N"` format: 32 hex characters with 122 random bits. That alone makes guessing infeasible; even at a million guesses a second, finding one live code takes far longer than its 7-day lifetime. This decision adds a second layer: a cap on wrong guesses, so the endpoints can't be used as a guessing oracle at all, and so a scripted attacker is noticed in the logs.

**Mechanism:** a new Application port `IInviteCodeAttemptLimiter` with two calls:
- `IsBlocked(ip)`: whether this IP has used up its wrong-guess budget in the current window.
- `RecordFailure(ip)`: uses up one unit of that budget.

It is implemented in Infrastructure as a singleton with `System.Threading.RateLimiting`, using a fixed window partitioned by IP, mirroring `ForgotPasswordRateLimiter`. Defaults are **10 wrong codes per IP per 15 minutes**, kept as constants like the reset limiter.

**What counts as a wrong guess:**
- An unknown code (404).
- A code that doesn't match `^[0-9a-f]{32}$`. These are rejected as 404 before any database lookup.

An expired code (410) does **not** count. It was a real code someone handed out, so a family member with an old link isn't penalised.

**Where it applies:** every path that takes a code: `POST /api/auth/register` (when `inviteCode` is present), `POST /api/invitations/{code}/join`, and the `GET /api/invitations/{code}` check. They share one counter, so an attacker can't widen the budget by switching endpoints. The check endpoint matters most here, because it is anonymous and cheap to call.

**Order of operations:**
1. If the IP is blocked, return **429** straight away, without looking up the code. A correct guess made while blocked is refused too, so the response never confirms a hit.
2. Otherwise run the use case.
3. On a wrong guess, record the failure and return the normal 404.

The 429 body is generic: `Too many invitation attempts. Please try again later.`

**Logging:** when an IP first exceeds its budget in a window, log a warning with the IP.

**Placement:** the check sits in the API layer, through a small shared helper used by `AuthController` and `GroupsController`, the same way `forgot-password` does its limiter check. Use cases keep signalling outcomes through domain exceptions and stay free of HTTP concerns such as client IPs.

The client IP comes from `HttpContext.Connection.RemoteIpAddress`. That is already the real client IP when deployed, because `Program.cs` trusts the ingress's `X-Forwarded-For`.

*Alternatives considered:*
- ASP.NET Core's `UseRateLimiter` endpoint policies. They count every request, not just failures, so real users sharing a NAT would be throttled on normal use; they also can't see the use-case outcome.
- Per-user limits on the join endpoint. Join already requires an account, and per-IP covers both paths with one counter.
- Persisting attempts in Postgres. Not needed with `maxReplicas: 1` (see Risks).

## Risks / Trade-offs

- [The attempt limiter is in-memory, so counters reset on restart or scale-to-zero, and wouldn't be shared across replicas] → The Container App is capped at `maxReplicas: 1`. An attacker who keeps sending guesses keeps the replica awake, so its counters persist. If the app ever scales out, move the counters to a shared store. The 122-bit codes remain the primary defense either way.
- [Several real users behind one NAT or IP mistyping codes could lock each other out for 15 minutes] → Only *wrong* codes count, and 10 per 15 minutes is generous for people pasting links. Expired links don't count.
- [An attacker rotating many IPs gets 10 guesses per IP] → Still negligible against a 122-bit space. The lockout warnings make a distributed attempt visible.

- [A leaked invite link lets strangers sign up for 7 days] → Accepted. Codes are 128-bit random, and the link has to be shared to leak. Invite revocation is a possible follow-up.
- [Bootstrap race on a fresh database: someone else could register first] → Only relevant before the first account exists, and the owner controls when ingress is enabled. The documented procedure is to register immediately after first deploy, or locally against the database before enabling ingress.
- [Breaking API change for any client that registers without a code] → The only client is our own SPA, which ships in the same deploy.
- [Two parallel bootstrap registrations could both pass `AnyAsync()`] → Harmless. Both are legitimate attempts in a window the owner controls, and the unique-email constraint still holds.

## Migration Plan

1. Merge and deploy as usual. No database migration is needed. The site stays offline during the deploy: reject the stale pending deploys, run `infra` to re-enable ingress, then approve the deploy so its smoke check can pass.
2. Verify against the live URL:
   - `/register` without a code shows "invitation only".
   - `POST /api/auth/register` without a code returns 403.
   - An invite link from the owner's group lets a test account register and lands it in the group.
3. Rollback: redeploy the previous image. Nothing needs to be cleaned up in the database.

## Open Questions

- **Planned follow-up: remove the first-account bootstrap completely.** Decision 3 is a stop-gap. A future iteration will delete the codeless "zero users" path, so registration *always* requires an invitation, and `GET /api/auth/registration` can then go too. That iteration needs another way to create the first account on a fresh database, for example a CLI or seed command run against the database. Keep the bootstrap code isolated, so it can be removed cleanly.
- None blocking. Single-use or revocable invites are left for a follow-up if a link ever leaks.
