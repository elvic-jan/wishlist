## MODIFIED Requirements

### Requirement: User registration
The system SHALL allow a new user to create an account with a unique email address and a display name only when the request carries a valid, unexpired group invitation code. The single exception is first-account bootstrap: while no user accounts exist at all, the system SHALL allow registration without an invitation code. Registering with an invitation code SHALL also make the new user a member of that invitation's group.

#### Scenario: Successful registration with an invitation
- **WHEN** a visitor submits a valid email, display name, password, and an existing, unexpired invitation code
- **THEN** the system creates the account, adds the user as a member of the invitation's group, marks the invitation as used, issues a JWT access token and an HttpOnly refresh token cookie, and returns the user's profile together with the joined group's ID

#### Scenario: Registration without an invitation rejected
- **WHEN** at least one user account exists and a visitor submits otherwise valid registration data without an invitation code
- **THEN** the system returns a 403 Forbidden error stating that registration is by invitation only, and does not create an account

#### Scenario: Unknown invitation code rejected
- **WHEN** a visitor submits registration data with an invitation code that does not exist
- **THEN** the system returns a 404 Not Found error and does not create an account

#### Scenario: Expired invitation code rejected
- **WHEN** a visitor submits registration data with an invitation code past its expiry date
- **THEN** the system returns a 410 Gone error and does not create an account

#### Scenario: First account bootstrap
- **WHEN** no user accounts exist and a visitor submits a valid email, display name, and password without an invitation code
- **THEN** the system creates the account without any group membership, issues a JWT access token and an HttpOnly refresh token cookie, and returns the user's profile

#### Scenario: Account and membership are created atomically
- **WHEN** registration with a valid invitation code fails while adding the group membership
- **THEN** the system does not persist the new account, so no account exists without its group membership

#### Scenario: Duplicate email rejected
- **WHEN** a visitor with a valid invitation code (or during bootstrap) submits an email address already associated with an existing account
- **THEN** the system returns a 409 Conflict error and does not create a new account

#### Scenario: Email existence not revealed without an invitation
- **WHEN** at least one user account exists and a visitor without a valid invitation code submits an email address already associated with an existing account
- **THEN** the system returns the same invitation error (403, 404, or 410) it would return for an unregistered email, not 409 Conflict

#### Scenario: Invalid input rejected
- **WHEN** a visitor submits a missing or malformed email, an empty display name, or a password shorter than 8 characters
- **THEN** the system returns a 400 Bad Request with field-level validation errors and does not create an account

## ADDED Requirements

### Requirement: Registration status endpoint
The system SHALL expose a public, unauthenticated endpoint that reports whether new registrations require an invitation code, so the client can show either the sign-up form or an "invitation only" message.

#### Scenario: Invitation required once any account exists
- **WHEN** at least one user account exists and a client requests the registration status
- **THEN** the system returns 200 OK with `inviteRequired: true`

#### Scenario: Bootstrap open on an empty system
- **WHEN** no user accounts exist and a client requests the registration status
- **THEN** the system returns 200 OK with `inviteRequired: false`

### Requirement: Sign-up UI is invitation-driven
The web client SHALL NOT offer open self-registration. It SHALL guide newcomers to create an account through an invitation link, and it SHALL route a newly registered invitee straight to the group they joined.

#### Scenario: Signed-out visitor opens an invitation link
- **WHEN** a signed-out visitor opens `/join/:code`
- **THEN** the page offers both "Create account", which leads to the registration form carrying the invitation code, and "Sign in", which returns to the invitation after login

#### Scenario: Unusable invitation reported before the form
- **WHEN** a signed-out visitor opens `/join/:code` or `/register?invite=:code` with an invitation code that is unknown, expired, or refused because of too many wrong attempts
- **THEN** the page shows why the invitation can't be used, and offers neither the "Create account" option nor the registration form

#### Scenario: Invitee completes registration
- **WHEN** a visitor submits the registration form that carries an invitation code and the server accepts it
- **THEN** the client signs the user in and navigates to the joined group's page

#### Scenario: Registration page opened without an invitation
- **WHEN** a visitor opens `/register` without an invitation code and the registration status reports `inviteRequired: true`
- **THEN** the page shows an "invitation only" message with a link to sign in, and does not show the registration form

#### Scenario: Login page does not advertise open registration
- **WHEN** a visitor views the login page
- **THEN** the page does not link to an open registration form and instead explains that new accounts are created from an invitation link
