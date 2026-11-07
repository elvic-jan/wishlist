## Context

Kadoozle has no site-wide role: the only roles are per circle (owner/member). ASP.NET Identity roles are registered (`AddRoles<IdentityRole<Guid>>`) but never used. The site owner gets into the site with the `owner-access` job, which prints a recovery link.

Nothing records when people use the app. `User.CreatedAt` exists, so sign-ups per month can be shown for the whole history. Sign-ins and visits cannot. Access tokens last 15 minutes and refresh tokens 30 days, rotating on every refresh. A regular user therefore signs in rarely and refreshes often, so "last sign-in" alone would make active people look inactive.

There are five ways to start a session: password login, passkey sign-in, account recovery (passkey or password reset), registration, and passkey sign-up. Four of them go through `AuthSessionFactory`. `LoginUserUseCase` builds its tokens itself.

## Goals / Non-Goals

**Goals:**
- Only accounts holding a role granted from the command line can see admin data, and the server enforces it.
- Record sign-ins and active days cheaply and with as little personal data as possible.
- One admin page with totals, growth and a users list, built so #88 and #121 can add sections later.

**Non-Goals:**
- Any write action from the admin page.
- Browser, device, IP or page-view tracking (#121).
- Back-filling activity from before the deploy.
- Pagination: the user base is small (hundreds of accounts), so the list loads in one request and is filtered in the browser.

## Decisions

### Admin role: Identity role "Admin", granted by a job
`--job admin --email <e> [--remove]` creates the role if it's missing, then adds or removes it with `UserManager`. This follows the `owner-access` pattern: an operator with database access is the trust anchor, and the app has no endpoint that can make anyone an admin.
- *Alternative: an email list in config.* Rejected: adding an admin would need a config change and a restart, and the list would be easy to get wrong between staging and production.

### Admin flag in the token, checked again on the server
`TokenService.GenerateAccessToken` adds `role: Admin` for admins. `AdminController` uses `[Authorize(Roles = "Admin")]`. The domain `User` gets `IsAdmin`, filled by the repository from the user's roles, so token creation and `UserDto` (`isAdmin`) share one source.
- A removed admin keeps access until their current access token expires, at most 15 minutes. That is acceptable for read-only data, so no per-request role lookup is needed.
- The frontend uses `isAdmin` only to show the menu item and route. The API is the actual gate.

### Session start goes through one place
`LoginUserUseCase` moves onto `AuthSessionFactory`. `AuthSessionFactory.CreateAsync` takes a `SignInMethod` (`Password`, `Passkey`, `Recovery`, `SignUp`) and records the sign-in and that day's activity through a new `IActivityRecorder`. Every way of signing in is then counted without repeating code in each flow.

### Two small tables
- `SignInEvents (Id, UserId, OccurredAt, Method)`, with an index on `(UserId, OccurredAt)` and cascade delete with the user. Last sign-in and number of sign-ins are computed from it. A row per sign-in also keeps the method, which #121 will want.
  - *Alternative: `LastSignInAt` and `SignInCount` columns on the user.* Cheaper, but it loses the method breakdown and per-period counts. At this volume the table stays tiny.
- `UserActivityDays (UserId, Day date)`, with a primary key on `(UserId, Day)` and cascade delete. Rows are written on session start and on token refresh with an insert that ignores conflicts (`ON CONFLICT DO NOTHING`), so a day is stored once whatever the number of refreshes. Active users over 7/30 days is a `COUNT(DISTINCT UserId)` over a date range. Last active is `MAX(Day)`.
  - *Alternative: a `LastSeenAt` column.* That can't produce "active in the previous 30 days" for the growth comparison.
- Recording errors are logged and swallowed. A failed insert must never block a sign-in or a refresh.

### Reading: application queries, EF implementation
`IAdminQueries` (Application) exposes `GetOverviewAsync(today)` and `GetUsersAsync()`, returning DTOs. `AdminQueries` in Infrastructure reads the `DbContext` directly, including Identity tables for passkeys (`AspNetUserPasskeys`) and password hashes, with one grouped query per figure and no per-user loops. The use cases `GetAdminOverviewUseCase` and `ListAdminUsersUseCase` are thin and take "now" from `TimeProvider` (as the messages use cases do) so tests can fix the date.
- Months and days are in UTC, the same as the reservation schedule.
- An active reservation is a `GiftClaim` with status Reserved that hasn't expired, the same rule as `IsActive`.

### Frontend
The `AdminPage` is loaded lazily at `/admin` and guarded by `isAdmin`; anyone else is redirected to `/wishlists`. It has three sections: totals as stat tiles; growth as tiles with a change arrow (▲/▼ and a number) plus a 12-month sign-ups bar list in plain CSS; and the users table with a text filter and a sort toggle. On a phone the table becomes stacked cards. The tracking start date comes from the overview (`trackingSince` = the earliest `SignInEvents`/`UserActivityDays` row, or null) and is shown as a note.

## Risks / Trade-offs

- [Activity numbers start at zero after deploy and look low for the first month] → the page shows "Tracking since <date>", and the comparison with the previous period shows "—" until there is a full previous period.
- [A refresh now also writes to the database] → it's one insert-or-ignore per refresh, and refreshes happen at most every 15 minutes per device. Negligible.
- [Removed admin keeps access for up to 15 minutes] → accepted, and documented in the job's help.
- [Email addresses on an admin page] → admin-only. The data is the same as what the operator can already query, and nothing is cached by the service worker (the API is `no-store`, like the other auth responses).
- [Staging resets its database when a migration is missing] → expected. After the reset, run the admin job on staging again.

## Migration Plan

1. Deploy: the migration adds the two tables, and default privileges give `wishlist_app` access.
2. Run `--job admin --email <owner>` on staging, check the page, then do the same on production after the production deploy is approved.
3. Rollback: redeploy the previous image. The new tables are unused by old code and can stay.

## Open Questions

- None blocking. The release note wording needs your OK before the PR.
