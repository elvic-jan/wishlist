## 1. Data model

- [x] 1.1 Add `SignInMethod` enum and `SignInEvent` / `UserActivityDay` entities (Domain)
- [x] 1.2 Configure both in `WishlistDbContext` (indexes, `(UserId, Day)` key, cascade delete with the user)
- [x] 1.3 Add the EF migration; confirm `infra/sql/app-role.sql` default privileges cover the new tables (no grant change expected)

## 2. Recording sign-ins and activity

- [x] 2.1 Add `IActivityRecorder` (Application) with `RecordSignInAsync(userId, method)` and `RecordActiveDayAsync(userId)`; implement in Infrastructure with insert-or-ignore for active days; log and swallow errors
- [x] 2.2 Give `AuthSessionFactory.CreateAsync` a `SignInMethod` and record the sign-in plus the active day there
- [x] 2.3 Move `LoginUserUseCase` onto `AuthSessionFactory` (method `Password`); pass the right method from passkey sign-in, recovery, password reset (if it signs in), register and passkey sign-up
- [x] 2.4 Record an active day in `RefreshTokenUseCase` after a successful exchange

## 3. Admin role

- [x] 3.1 Add `IsAdmin` to the domain `User`, filled by the user repository from Identity roles
- [x] 3.2 Add the Admin role claim in `TokenService.GenerateAccessToken`; add `isAdmin` to `UserDto` and every place that builds it (incl. `GET /api/auth/me`)
- [x] 3.3 Add `AdminRoleUseCase` (grant/remove, create role if missing) and the `--job admin --email <e> [--remove]` case in `Program.cs`, including the unknown-job message
- [x] 3.4 Document the job in `infra/README.md` next to "Owner access" (how to run it on staging and production; role change applies within 15 minutes)

## 4. Admin queries and API

- [x] 4.1 Add admin DTOs (`AdminOverviewDto` with totals, monthly sign-ups, active 7/30 with previous periods, `trackingSince`; `AdminUserDto`) and `IAdminQueries` (Application)
- [x] 4.2 Implement `AdminQueries` in Infrastructure with grouped queries (users, circles, wishes, active reservations; passkeys via Identity passkey table; circles per user; sign-in count/last; last active day)
- [x] 4.3 Add `GetAdminOverviewUseCase` and `ListAdminUsersUseCase` using `TimeProvider`; register in DI
- [x] 4.4 Add `AdminController` (`[Authorize(Roles = "Admin")]`, `GET /api/admin/overview`, `GET /api/admin/users`, `Cache-Control: no-store`)

## 5. Frontend

- [x] 5.1 Add `isAdmin` to the auth user type and an admin API client (`api/admin.ts`)
- [x] 5.2 Add the "Admin" account-menu item in `AppShell`, shown only for admins
- [x] 5.3 Add lazy `/admin` route with a guard that redirects non-admins to `/wishlists`
- [x] 5.4 Build `AdminPage`: totals tiles, growth tiles with change (▲/▼ or "—") and a 12-month sign-ups bar list, "Tracking since" note
- [x] 5.5 Build the users table: text filter, sort by sign-up date / last active, "—" for missing values, stacked cards at phone width

## 6. Tests

- [x] 6.1 API tests: admin endpoints return 401 without token, 403 for a non-admin, 200 for an admin
- [x] 6.2 Tests: each sign-in method records one sign-in with the right method; failed sign-in records nothing; refresh records an active day; repeated refreshes on one day store one row; recording failure doesn't break sign-in
- [x] 6.3 Tests: overview figures (totals, 12 months with zero months, 7/30-day active and previous periods, "—" without a full previous period) with a fixed `TimeProvider`
- [x] 6.4 Tests: admin job grant / already admin / remove / unknown email / missing email; `isAdmin` in `/me` and the token
- [x] 6.5 Run backend tests, frontend lint and build

## 7. Release

- [x] 7.1 Add `release-notes/site-admin-page.md` (type `feature`): "New admin page for site administrators - that's not you ;-)" (confirmed by the user)
- [x] 7.2 Deploy the branch to staging (`gh workflow run deploy.yml --ref feat/admin-page`), run the admin job there, check the page on desktop and phone width and that a non-admin gets no access
- [x] 7.3 Open the PR with "Closes #104" and the release note in the description
- [ ] 7.4 After the production deploy: run the admin job on production for the owner
