## Why

Running Kadoozle today means guessing: there is no place to see who has an account, whether people come back, or whether the site is growing. Answering that takes a database query. A site-wide admin page (issue #104) gives the site owner that view, and later features (feedback inbox #88, usage statistics #121) can add sections to it.

## What Changes

- New **site admin role**: an "Admin" role on accounts, separate from circle owners. It is granted and removed only by a new command-line job (`--job admin --email <email> [--remove]`), never from the app.
- The signed-in user's info (sign-in response and `GET /api/auth/me`) and the access token say whether the user is an admin. Admins see an "Admin" item in the account menu.
- New **sign-in and activity tracking**, starting when this ships (no history before that):
  - every fresh sign-in (password, passkey, account recovery, sign-up) is recorded with its time and method;
  - each user's active days are recorded, at most once per day, on sign-in and when the app renews the session. Sessions last 30 days, so most visits never involve a new sign-in.
  - Nothing else is stored: no IP address, browser or device.
- New read-only **admin page** at `/admin`:
  - **Totals**: users, circles, wishes, active reservations.
  - **Growth**: new sign-ups this month vs last month and per month for the last 12 months; active users in the last 7 and 30 days, compared with the 7 and 30 days before.
  - **Users**: name, email, signed-up date, email confirmed, sign-in options (password, passkey), number of circles, last sign-in, number of sign-ins, last active; filter by text and sort by sign-up date or last active.
- New admin-only API: `GET /api/admin/overview` and `GET /api/admin/users`. Anyone who isn't an admin gets 403.

Non-goals: any action that changes data from the admin page (deleting users, sending sign-in links), the feedback inbox (#88), browser and device statistics (#121), circle admin roles (#93).

## Capabilities

### New Capabilities
- `site-admin`: the site admin role and its job, the admin page (totals, growth, users list) and its access rules.

### Modified Capabilities
- `user-auth`: sign-ins and active days are recorded; the user info and access token carry the admin flag. These are added as new requirements, so existing requirements stay unchanged.

## Impact

- **Database**: one EF migration with two new tables (sign-in events and user activity days). The least-privilege app login already gets rights on new tables through its default privileges.
- **Backend**: auth use cases (password login moves onto the shared session factory, and token refresh records activity), token service (role claim), user DTO, a new `AdminController`, application queries plus an EF implementation, and the new job in `Program.cs`.
- **Frontend**: an admin page, a lazy route, and the account-menu item in `AppShell`.
- **Ops**: `infra/README.md` documents the admin job. After deploy, run the job on staging and production to make the owner an admin.
- **Release note**: `release-notes/site-admin-page.md` (feature).
