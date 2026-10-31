## MODIFIED Requirements

### Requirement: User registration
The system SHALL allow a new user to create an account with a unique email address and a display name only when the request carries a valid, unexpired group invitation code. This SHALL hold even when no user accounts exist. Registering SHALL also make the new user a member of that invitation's group.

#### Scenario: Successful registration with an invitation
- **WHEN** a visitor submits a valid email, display name, password, and an existing, unexpired invitation code
- **THEN** the system creates the account, adds the user as a member of the invitation's group, marks the invitation as used, issues a JWT access token and an HttpOnly refresh token cookie, and returns the user's profile together with the joined group's ID

#### Scenario: Registration without an invitation rejected
- **WHEN** a visitor submits otherwise valid registration data without an invitation code
- **THEN** the system returns a 403 Forbidden error stating that registration is by invitation only, and does not create an account

#### Scenario: No codeless sign-up on an empty system
- **WHEN** no user accounts exist and a visitor submits a valid email, display name, and password without an invitation code
- **THEN** the system returns a 403 Forbidden error stating that registration is by invitation only, and does not create an account

#### Scenario: Unknown invitation code rejected
- **WHEN** a visitor submits registration data with an invitation code that does not exist
- **THEN** the system returns a 404 Not Found error and does not create an account

#### Scenario: Expired invitation code rejected
- **WHEN** a visitor submits registration data with an invitation code past its expiry date
- **THEN** the system returns a 410 Gone error and does not create an account

#### Scenario: Account and membership are created atomically
- **WHEN** registration with a valid invitation code fails while adding the group membership
- **THEN** the system does not persist the new account, so no account exists without its group membership

#### Scenario: Duplicate email rejected
- **WHEN** a visitor with a valid invitation code submits an email address already associated with an existing account
- **THEN** the system returns a 409 Conflict error and does not create a new account

#### Scenario: Email existence not revealed without an invitation
- **WHEN** a visitor without a valid invitation code submits an email address already associated with an existing account
- **THEN** the system returns the same invitation error (403, 404, or 410) it would return for an unregistered email, not 409 Conflict

#### Scenario: Invalid input rejected
- **WHEN** a visitor submits a missing or malformed email, an empty display name, or a password shorter than 8 characters
- **THEN** the system returns a 400 Bad Request with field-level validation errors and does not create an account

### Requirement: Passkey registration through an invitation
The system SHALL let a visitor create an account with a passkey instead of a password, under the same rules as password registration: a valid invitation is always required, the email must not already be registered, and the account joins the invitation's circle in the same operation.

#### Scenario: Successful passkey sign-up with an invitation
- **WHEN** a visitor requests passkey sign-up options with a valid invitation code, an unused email and a display name, then submits a valid attestation with the same email and display name
- **THEN** the system creates an account without a password, stores the passkey, adds the account to the invitation's circle, marks the invitation used, returns an access token with the joined circle, sets the refresh-token cookie, and sends an email-confirmation message

#### Scenario: Invitation gate applies to passkey sign-up
- **WHEN** a visitor requests passkey sign-up options or submits a passkey sign-up without an invitation code, or with an unknown or expired code
- **THEN** the system responds exactly as password registration does (403, 404 or 410) and creates no account

#### Scenario: Details changed between options and completion
- **WHEN** a visitor submits a passkey sign-up whose email or display name differs from the ones the options were requested for
- **THEN** the system returns 400 Bad Request and creates no account

#### Scenario: Email taken before completion
- **WHEN** another account with the same email is created between the options request and the completion
- **THEN** the system returns 409 Conflict and creates no account

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
- **WHEN** a visitor opens `/register` without an invitation code
- **THEN** the page shows an "invitation only" message with a link to sign in, does not show the registration form, and does not ask the server whether registration is open

#### Scenario: Login page does not advertise open registration
- **WHEN** a visitor views the login page
- **THEN** the page does not link to an open registration form and instead explains that new accounts are created from an invitation link

## ADDED Requirements

### Requirement: Owner access job
The system SHALL provide a command-line job, `--job owner-access --email <email> [--name <name>]`, that runs against the configured database and exits without starting the web server. It SHALL create the account when none exists for the email and SHALL print a single-use account-recovery link for that account to the console. It SHALL NOT send any email. This job is the only way to create an account without an invitation.

#### Scenario: First account on a fresh install
- **WHEN** an operator runs the job with an email that has no account and a name
- **THEN** the system creates an account with that email and name, without a password and without any circle, marks the email confirmed, prints that it created the account, and prints a recovery link to `<App:PublicBaseUrl>/recover` with its expiry

#### Scenario: Getting back into an existing account
- **WHEN** an operator runs the job with the email of an existing account
- **THEN** the system leaves the account's details unchanged, prints that it found the account, and prints a recovery link for it with its expiry

#### Scenario: Printed link signs the owner in
- **WHEN** the owner opens the printed link before it expires and adds a passkey or sets a password there
- **THEN** recovery behaves as for an emailed recovery link: the owner is signed in, the link stops working and every other session ends

#### Scenario: Name missing for a new account
- **WHEN** an operator runs the job with an email that has no account and no `--name`
- **THEN** the job fails with a message asking for `--name` and creates nothing

#### Scenario: Email missing
- **WHEN** an operator runs the job without `--email`
- **THEN** the job fails with a message asking for `--email` and changes nothing

## REMOVED Requirements

### Requirement: Registration status endpoint
**Reason**: It only existed so the SPA could show a codeless sign-up form during first-account bootstrap, which this change removes. Registration now always needs an invitation, so there is nothing to report.
**Migration**: `GET /api/auth/registration` returns 404. Clients treat `/register` without a code as "invitation only". The first account on a fresh install is created with the `owner-access` job.
