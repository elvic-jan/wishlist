# site-admin Specification

## Purpose
A read-only, site-wide admin page for operators: totals, growth and the list of accounts, open only to accounts with the Admin role, which a command-line job grants.
## Requirements
### Requirement: Site admin role managed by a job
The system SHALL have a site-wide "Admin" role, separate from circle roles. The role SHALL be granted and removed only by a command-line job, `--job admin --email <email> [--remove]`, which runs against the configured database and exits without starting the web server. No API endpoint SHALL change who is an admin.

#### Scenario: Grant admin
- **WHEN** an operator runs the job with the email of an existing account
- **THEN** the account gets the Admin role (creating the role if it does not exist yet), and the job prints that the account is now an admin

#### Scenario: Grant to an account that is already admin
- **WHEN** an operator runs the job for an account that already has the Admin role
- **THEN** nothing changes and the job prints that the account was already an admin

#### Scenario: Remove admin
- **WHEN** an operator runs the job with `--remove` for an admin account
- **THEN** the account loses the Admin role and the job prints that it is no longer an admin

#### Scenario: Unknown email
- **WHEN** an operator runs the job with an email that has no account
- **THEN** the job fails with a message saying no account was found, exits with a non-zero code and creates nothing

#### Scenario: Email missing
- **WHEN** an operator runs the job without `--email`
- **THEN** the job fails with a message asking for `--email` and changes nothing

### Requirement: Admin data only for admins
The admin endpoints (`GET /api/admin/overview`, `GET /api/admin/users`) SHALL answer only requests from a signed-in user whose access token carries the Admin role. Responses SHALL NOT be cached.

#### Scenario: Admin reads the overview
- **WHEN** a signed-in admin calls `GET /api/admin/overview`
- **THEN** the system returns 200 with the totals and growth figures

#### Scenario: Signed-in non-admin
- **WHEN** a signed-in user without the Admin role calls an admin endpoint
- **THEN** the system returns 403 and no admin data

#### Scenario: Not signed in
- **WHEN** a request without a valid access token calls an admin endpoint
- **THEN** the system returns 401

### Requirement: Admin page entry
The app SHALL show an "Admin" item in the account menu only to admins, opening the admin page at `/admin`. A non-admin who opens `/admin` SHALL be sent to the wishlists page.

#### Scenario: Admin sees the menu item
- **WHEN** an admin opens the account menu
- **THEN** an "Admin" item is shown and opens `/admin`

#### Scenario: Non-admin
- **WHEN** a user without the Admin role opens the account menu or goes to `/admin`
- **THEN** no "Admin" item is shown, and `/admin` redirects to `/wishlists`

### Requirement: Admin totals
The admin page SHALL show the current number of user accounts, circles, wishes, and active reservations (reserved, not bought, not expired).

#### Scenario: Totals shown
- **WHEN** an admin opens the admin page
- **THEN** the page shows the four totals as they are at that moment

### Requirement: Admin growth indicators
The admin page SHALL show growth figures, using UTC calendar months and days:
- new sign-ups in the current month and in the previous month;
- new sign-ups per month for the last 12 months including the current one (months without sign-ups show 0), from the accounts' creation dates;
- active users in the last 7 days and the last 30 days, each compared with the 7 or 30 days before, where an active user has at least one recorded active day in the period.

#### Scenario: Sign-ups per month
- **WHEN** an admin opens the admin page
- **THEN** the page shows sign-ups for each of the last 12 months, oldest first, and this month's sign-ups next to last month's

#### Scenario: Active users compared
- **WHEN** 12 users were active in the last 7 days and 9 in the 7 days before that
- **THEN** the page shows 12 active users for 7 days and an increase of 3

#### Scenario: No full previous period yet
- **WHEN** activity tracking started less than one full comparison period ago
- **THEN** the page shows the current figure and "—" instead of a change

#### Scenario: Tracking start shown
- **WHEN** activity has been recorded
- **THEN** the page shows "Tracking since <date of the first recorded activity>"; without any recorded activity it shows that tracking hasn't recorded anything yet

### Requirement: Admin users list
The admin page SHALL list every user account with: display name, email, sign-up date, whether the email is confirmed, sign-in options (password, passkey, or both), number of circles, last sign-in time, number of sign-ins, and last active day. Values with nothing recorded SHALL show "—". The list SHALL be filterable by text matching name or email, and sortable by sign-up date and by last active (newest first by default on sign-up date). The list SHALL be read-only.

#### Scenario: List shown
- **WHEN** an admin opens the admin page
- **THEN** every account appears once with the listed fields

#### Scenario: User never signed in since tracking began
- **WHEN** an account has no recorded sign-ins or active days
- **THEN** last sign-in, last active show "—" and number of sign-ins shows 0

#### Scenario: Filter
- **WHEN** the admin types "anna" in the filter
- **THEN** only accounts whose name or email contains "anna" (ignoring case) are shown

#### Scenario: Sort by last active
- **WHEN** the admin sorts by last active
- **THEN** accounts are ordered by last active day, most recent first, with accounts without activity last

#### Scenario: Phone width
- **WHEN** the admin page is opened on a phone
- **THEN** each user shows as a card with the same fields and the page does not scroll sideways

