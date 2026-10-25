## 1. Application layer

- [x] 1.1 Add optional `InviteCode` to `RegisterRequest`, and optional `JoinedGroupId` (`Guid?`) to `AuthResponse` (login leaves it null)
- [x] 1.2 Add `AnyAsync()` to `IUserRepository` and implement it in `UserRepository`
- [x] 1.3 Add an `IUnitOfWork` port with `ExecuteInTransactionAsync(Func<CancellationToken, Task>, CancellationToken)` in Application
- [x] 1.4 Rework `RegisterUserUseCase`:
  - Validation order: input validation (400), then the invite gate (403 if users exist and no code; 404 unknown code; 410 expired code), then the duplicate-email check (409)
  - For invited sign-ups, create the user, add the `Member` membership and stamp `UsedAt` inside one transaction
  - Return `JoinedGroupId`
- [x] 1.5 Add a `GetRegistrationStatusUseCase` (or a small query on the use case) that returns `inviteRequired = await users.AnyAsync()`

## 2. Infrastructure & API

- [x] 2.1 Implement `IUnitOfWork` over `WishlistDbContext` using the execution strategy and `BeginTransactionAsync`, and register it in DI
- [x] 2.2 Confirm the Identity `UserManager.CreateAsync` and group/invitation repository saves all join the ambient transaction (same scoped `WishlistDbContext`)
- [x] 2.3 Add `[AllowAnonymous] GET /api/auth/registration` returning `{ inviteRequired }`
- [x] 2.4 Add an `IInviteCodeAttemptLimiter` port (`IsBlocked(ip)`, `RecordFailure(ip)`) and a singleton `InviteCodeAttemptLimiter` in Infrastructure: a fixed window partitioned by IP, 10 wrong codes per 15 min, modelled on `ForgotPasswordRateLimiter`. Log a warning the first time an IP exceeds its budget in a window
- [x] 2.5 Reject codes not matching `^[0-9a-f]{32}$` as 404 before any database lookup (in the invite lookup path shared by register and join)
- [x] 2.6 Add a shared API helper that checks the limiter before register-with-code and join, returns 429 `Too many invitation attempts. Please try again later.` when the IP is blocked, and records a failure on `NotFoundException` (not on `GoneException`); wire it into `AuthController.Register` and `GroupsController` join
- [x] 2.7 Add an anonymous `GET /api/invitations/{code}` check (200 with `expiresAt` / 404 / 410) through the invite-code guard, without marking the invite used
- [x] 2.8 Check that `ForbiddenException`, `NotFoundException` and `GoneException` from register come back as 403/404/410 through the existing exception middleware

## 3. API tests

- [x] 3.1 Update `AuthSessionCookieTests` so its registrations use a valid invite (or rely on bootstrap for the first user)
- [x] 3.2 Test: with an existing user, register without a code → 403 and no account created
- [x] 3.3 Test: unknown code → 404; expired code → 410; neither creates an account
- [x] 3.4 Test: valid code → 200, refresh cookie set, user is a member of the invite's group, `joinedGroupId` returned, invite `UsedAt` stamped
- [x] 3.5 Test: the same invite code can register a second, different user before expiry
- [x] 3.6 Test: empty database → register without a code succeeds with no group; a second codeless registration → 403
- [x] 3.7 Test: an already-registered email without a valid code gets the invite error, not 409; with a valid code it gets 409
- [x] 3.8 Test: if adding the membership fails, no user row remains (atomicity)
- [x] 3.9 Test: 10 unknown codes from one IP → 404s; the 11th attempt returns 429, also on the other endpoint (register vs join), and also for a valid code, with no account or membership created
- [x] 3.10 Test: expired codes don't count toward the limit; a malformed code returns 404 and does count; a different IP is unaffected by another IP's block
- [x] 3.11 Test: the block lifts after the window (inject a short window or a time provider for the test)
- [x] 3.12 Test: the invitation check reports 200/404/410 anonymously without marking the invite used, and shares (and is blocked by) the guess budget
- [x] 3.13 Test: `GET /api/auth/registration` returns `inviteRequired: false` when empty and `true` once a user exists

## 4. Web client

- [x] 4.1 `AuthContext.register` accepts an optional `inviteCode` and returns `joinedGroupId`
- [x] 4.2 `RegisterPage`:
  - Read `?invite=` and send it; show an "You've been invited to join a group" hint
  - On success navigate to `/groups/<joinedGroupId>` (or `/` when there is none)
  - Show the server message for 404/410 with advice to ask for a new link, and for 429 tell the user to wait and try again later (also on `JoinGroupPage`)
- [x] 4.3 `RegisterPage` without a code: fetch `GET /api/auth/registration`; when `inviteRequired`, render an "invitation only" card with a Sign in link instead of the form
- [x] 4.4 `JoinGroupPage` signed-out state: add a "Create account" button to `/register?invite=<code>` next to "Sign in"
- [x] 4.5 `LoginPage`: replace the Register link with "New here? Ask a family member for an invite link."
- [x] 4.6 Check the code on load in `JoinGroupPage` (signed out) and `RegisterPage` (with `?invite=`), and show an "invitation can't be used" card instead of the sign-up options when it fails
- [x] 4.7 Run lint, type-check and build for `src/Wishlist.Web`

## 5. Verify & ship

- [x] 5.1 Local end-to-end in the browser:
  - Owner creates a group invite
  - Signed-out window opens `/join/<code>` → Create account → lands on the group page as a member
  - `/register` with no code shows "invitation only"
- [x] 5.2 Open the PR and merge to `main` (PR #11, 2026-10-24)
- [x] 5.3 Bring the site back online (2026-10-24; the infra run first needed the Postgres ordering fix, PR #12):
  - Reject the stale pending deploys from #9/#10
  - Run `infra` to re-enable ingress
  - Approve the deploy (it ships #9, #10 and this change)
- [x] 5.4 Verify on the live URL (403 and `inviteRequired` checked 2026-10-24/25; live invite sign-up confirmed by the owner 2026-10-25):
  - `POST /api/auth/register` without a code returns 403
  - `GET /api/auth/registration` returns `inviteRequired: true`
  - An invite link from the owner's group registers a test account into the group
