## MODIFIED Requirements

### Requirement: User login
The system SHALL authenticate a registered user by email and password and issue tokens. An unknown email address SHALL take about as long to refuse as a wrong password, so response times don't reveal which addresses have an account.

#### Scenario: Successful login
- **WHEN** a registered user submits correct email and password credentials
- **THEN** the system returns a JWT access token and sets an HttpOnly refresh token cookie

#### Scenario: Invalid credentials rejected
- **WHEN** a user submits an unrecognised email or incorrect password
- **THEN** the system returns a 401 Unauthorised error and does not issue tokens

#### Scenario: Unknown email checks a password too
- **WHEN** a user signs in with an email address that has no account
- **THEN** the system still verifies the password against a stored-format hash before refusing

### Requirement: Token refresh
The system SHALL allow a client to obtain a new access token using a valid refresh token without re-entering credentials. The system SHALL store only a SHA-256 hash of each refresh token, never the token itself. Rotation SHALL be atomic: a refresh token SHALL produce at most one new token, even when presented by several requests at once. When issuing a refresh token, the system SHALL delete the user's refresh tokens that expired or were revoked more than a day earlier.

#### Scenario: Successful refresh
- **WHEN** a client sends a request to the refresh endpoint with a valid, unexpired HttpOnly refresh token cookie
- **THEN** the system issues a new JWT access token and rotates the refresh token

#### Scenario: Expired or invalid refresh token rejected
- **WHEN** a client presents an expired, revoked, or tampered refresh token
- **THEN** the system returns a 401 Unauthorised error and clears the refresh token cookie

#### Scenario: Database holds no usable tokens
- **WHEN** someone reads the refresh token table
- **THEN** it contains only hashes, and presenting a stored value as a cookie is rejected

#### Scenario: Sessions survive the switch to hashes
- **WHEN** a user has a refresh token issued before tokens were hashed
- **THEN** that token still refreshes after the upgrade

#### Scenario: Same token used twice at once
- **WHEN** two refresh requests carry the same valid token at the same time
- **THEN** exactly one of them gets a new token and the other gets 401

#### Scenario: Several tabs restore the session together
- **WHEN** a user opens the app in two tabs at once
- **THEN** the SPA lets one tab refresh first and the other refresh afterwards with the new cookie, and both tabs stay signed in

#### Scenario: Old tokens are removed
- **WHEN** a user signs in or refreshes
- **THEN** their refresh tokens that expired, or were revoked more than a day ago, are deleted

### Requirement: User profile
The system SHALL allow an authenticated user to view and update their display name.

#### Scenario: View profile
- **WHEN** an authenticated user requests their profile
- **THEN** the system returns the user's display name and email

#### Scenario: Update display name
- **WHEN** an authenticated user submits a new non-empty display name
- **THEN** the system updates the display name and returns the updated profile

#### Scenario: Empty or overlong display name rejected
- **WHEN** an authenticated user submits an empty display name, or one longer than 100 characters
- **THEN** the system returns 400 Bad Request and keeps the old name

## ADDED Requirements

### Requirement: Account fields have length limits
Every sign-up path (password and passkey) SHALL refuse with 400 Bad Request, before creating anything, a display name longer than 100 characters, an email address longer than 256 characters, or a password longer than 256 characters. The SPA's inputs SHALL not accept more than these limits.

#### Scenario: Overlong display name at sign-up
- **WHEN** someone signs up with a 101-character display name
- **THEN** the system returns 400 with a message naming the limit, and no account is created

#### Scenario: Overlong email at sign-up
- **WHEN** someone signs up with an email address longer than 256 characters
- **THEN** the system returns 400, not 500
