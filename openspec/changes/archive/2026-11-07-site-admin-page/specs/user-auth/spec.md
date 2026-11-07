## ADDED Requirements

### Requirement: Sign-ins are recorded
Every new session SHALL be recorded with the user, the time and the method: `password` (password sign-in), `passkey` (passkey sign-in), `recovery` (signing in through account recovery or a password reset that signs the user in), or `signup` (registration with a password or a passkey). Token refresh SHALL NOT count as a sign-in. No IP address, browser or device information SHALL be stored. A failure to record SHALL NOT make the sign-in fail.

#### Scenario: Password sign-in recorded
- **WHEN** a user signs in with their password
- **THEN** a sign-in with method `password` and the current time is recorded for that user

#### Scenario: Passkey sign-up recorded
- **WHEN** a new user registers with a passkey through an invitation
- **THEN** a sign-in with method `signup` is recorded for the new account

#### Scenario: Failed sign-in not recorded
- **WHEN** a sign-in attempt fails (wrong password, unknown passkey)
- **THEN** no sign-in is recorded

#### Scenario: Recording fails
- **WHEN** storing the sign-in record fails
- **THEN** the error is logged and the user is still signed in

### Requirement: Active days are recorded
The system SHALL record, at most once per user per UTC day, that the user was active. A day SHALL be recorded when a session starts and when a refresh token is successfully exchanged. A failure to record SHALL NOT make the sign-in or refresh fail.

#### Scenario: Many refreshes in a day
- **WHEN** a user's app refreshes its token ten times on the same UTC day
- **THEN** exactly one active day is stored for that user and date

#### Scenario: Returning without signing in
- **WHEN** a user opens the app on a new day and the app refreshes the session with a valid refresh token
- **THEN** that day is recorded as active for the user

### Requirement: Signed-in user knows whether they are an admin
The user info returned on sign-in, registration, passkey sign-in, recovery and `GET /api/auth/me` SHALL include `isAdmin`. Access tokens issued to an admin SHALL carry the Admin role claim. A change of role SHALL take effect at the latest when the access token is next refreshed.

#### Scenario: Admin signs in
- **WHEN** a user with the Admin role signs in
- **THEN** the response has `isAdmin: true` and the access token carries the Admin role

#### Scenario: Role removed
- **WHEN** an admin's role is removed by the job while they are signed in
- **THEN** their next refreshed access token no longer carries the Admin role and admin endpoints return 403 from then on
