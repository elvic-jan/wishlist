## ADDED Requirements

### Requirement: User registration
The system SHALL allow a new user to create an account with a unique email address and a display name.

#### Scenario: Successful registration
- **WHEN** a visitor submits a valid email, display name, and password
- **THEN** the system creates the account, issues a JWT access token and an HttpOnly refresh token cookie, and returns the user's profile

#### Scenario: Duplicate email rejected
- **WHEN** a visitor submits an email address already associated with an existing account
- **THEN** the system returns a 409 Conflict error and does not create a new account

#### Scenario: Invalid input rejected
- **WHEN** a visitor submits a missing or malformed email, an empty display name, or a password shorter than 8 characters
- **THEN** the system returns a 400 Bad Request with field-level validation errors and does not create an account

### Requirement: User login
The system SHALL authenticate a registered user by email and password and issue tokens.

#### Scenario: Successful login
- **WHEN** a registered user submits correct email and password credentials
- **THEN** the system returns a JWT access token and sets an HttpOnly refresh token cookie

#### Scenario: Invalid credentials rejected
- **WHEN** a user submits an unrecognised email or incorrect password
- **THEN** the system returns a 401 Unauthorised error and does not issue tokens

### Requirement: Token refresh
The system SHALL allow a client to obtain a new access token using a valid refresh token without re-entering credentials.

#### Scenario: Successful refresh
- **WHEN** a client sends a request to the refresh endpoint with a valid, unexpired HttpOnly refresh token cookie
- **THEN** the system issues a new JWT access token and rotates the refresh token

#### Scenario: Expired or invalid refresh token rejected
- **WHEN** a client presents an expired, revoked, or tampered refresh token
- **THEN** the system returns a 401 Unauthorised error and clears the refresh token cookie

### Requirement: User logout
The system SHALL allow an authenticated user to end their session.

#### Scenario: Successful logout
- **WHEN** an authenticated user calls the logout endpoint
- **THEN** the system revokes the refresh token server-side and clears the HttpOnly cookie

### Requirement: User profile
The system SHALL allow an authenticated user to view and update their display name.

#### Scenario: View profile
- **WHEN** an authenticated user requests their profile
- **THEN** the system returns the user's display name and email

#### Scenario: Update display name
- **WHEN** an authenticated user submits a new non-empty display name
- **THEN** the system updates the display name and returns the updated profile
