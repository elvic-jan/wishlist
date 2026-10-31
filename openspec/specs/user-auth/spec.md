# user-auth Specification

## Purpose
TBD - created by archiving change family-gift-wishlist. Update Purpose after archive.
## Requirements
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

#### Scenario: Empty or overlong display name rejected
- **WHEN** an authenticated user submits an empty display name, or one longer than 100 characters
- **THEN** the system returns 400 Bad Request and keeps the old name

### Requirement: Authentication traffic served only over HTTPS

The system SHALL serve all authentication endpoints — registration, login, token refresh, and logout — exclusively over HTTPS in every environment, including local development. The HttpOnly refresh token cookie SHALL be issued with the `Secure` attribute so that browsers refuse to transmit it over plain HTTP.

In local development the API itself terminates TLS and binds no HTTP listener. In deployed environments TLS SHALL terminate at the hosting platform's ingress, which SHALL refuse or redirect plain-HTTP requests from the internet. The container behind it MAY listen on plain HTTP reachable only from that ingress, and the API SHALL derive the request scheme and client IP address from the ingress's `X-Forwarded-Proto` and `X-Forwarded-For` headers so that HTTPS enforcement and per-IP rate limiting act on the real client connection.

#### Scenario: HTTP request to an auth endpoint is refused

- **WHEN** a client sends a request to a registration, login, refresh, or logout endpoint over plain HTTP
- **THEN** the API does not process the credentials or token: in local development the request fails to connect (no HTTP listener is bound), and in deployed environments the platform ingress redirects it to HTTPS without forwarding the credentials or token to the API

#### Scenario: HTTPS request through the platform ingress is accepted

- **WHEN** a client calls an auth endpoint over HTTPS on the deployed origin and the ingress forwards it to the container over plain HTTP with `X-Forwarded-Proto: https`
- **THEN** the API treats the request as HTTPS and processes it normally

#### Scenario: Client IP is taken from the ingress

- **WHEN** two different clients call the password-reset request endpoint through the deployed ingress
- **THEN** the API attributes each request to that client's own IP address (from `X-Forwarded-For`), not to the ingress's address, so the per-IP rate limit counts them separately

#### Scenario: Refresh token cookie cannot leak over HTTP

- **WHEN** the API issues the HttpOnly refresh token cookie after a successful registration, login, or refresh
- **THEN** the cookie carries the `Secure` attribute so the browser only sends it back over HTTPS, and never over an HTTP request to the same host

### Requirement: SPA host that consumes auth cookies SHALL itself be served over HTTPS

To keep the `Secure`-flagged refresh-token cookie usable, any origin that issues authenticated requests against the API SHALL itself be served over HTTPS in every environment, including the local Vite dev server. The system SHALL NOT ship a default developer configuration where the SPA is served over plain HTTP while the API issues `Secure` cookies, because the browser will silently drop the cookie and break the user's session on the next reload.

#### Scenario: Local Vite dev server serves HTTPS

- **WHEN** a developer runs `npm run dev` for the SPA
- **THEN** the dev server binds an HTTPS listener (default `https://localhost:5173`) so the browser treats the SPA origin as secure and will store/send the API's `Secure` refresh-token cookie

#### Scenario: Browser receives Secure cookie via a secure origin

- **WHEN** a user logs in through the SPA and the `Set-Cookie: refreshToken=...; Secure; HttpOnly; SameSite=Strict` response is delivered through the dev proxy
- **THEN** the browser stores the cookie (because the response origin is HTTPS) and sends it on the next `POST /api/auth/refresh` request, so the session survives a page reload

### Requirement: Password reset request endpoint is enumeration-safe

The system SHALL expose a public endpoint that accepts an email address and triggers delivery of an account-recovery email if (and only if) the email belongs to a registered user that is eligible for recovery: its email is confirmed, or it has a password. The email SHALL link to a page that offers both adding a passkey and setting a new password. The endpoint's response SHALL NOT differ between the "known email", "ineligible account" and "unknown email" cases in either status code, response body, or response timing characteristics, so that an unauthenticated caller cannot use it to enumerate registered users.

#### Scenario: Request for a registered email

- **WHEN** an unauthenticated caller submits a syntactically valid email address that belongs to a registered user whose email is confirmed or who has a password
- **THEN** the system generates a single-use recovery token bound to that user, sends an email to the user containing a recovery link carrying the token, and returns 202 Accepted with an empty body

#### Scenario: Request for an unconfirmed passkey-only account

- **WHEN** an unauthenticated caller submits the email of an account that has no password and whose email is not confirmed
- **THEN** the system sends no email and returns 202 Accepted with an empty body — indistinguishable from the other cases

#### Scenario: Request for an unregistered email

- **WHEN** an unauthenticated caller submits a syntactically valid email address that does NOT belong to any registered user
- **THEN** the system sends no email, persists no token, and returns 202 Accepted with an empty body — indistinguishable from the registered-email response

#### Scenario: Request with a malformed email

- **WHEN** an unauthenticated caller submits a missing or syntactically invalid email
- **THEN** the system returns 400 Bad Request with a generic validation error and does not attempt any user lookup or email delivery

### Requirement: Password reset request endpoint is rate-limited

The system SHALL rate-limit the password-reset request endpoint per submitted email address and per client IP, returning 429 Too Many Requests when either limit is exceeded, so the endpoint cannot be used as an email-spam vector or as a password-spray oracle.

#### Scenario: Per-email rate limit exceeded

- **WHEN** an unauthenticated caller submits more than the configured number of password-reset requests for the same email address within the configured rolling window
- **THEN** the system returns 429 Too Many Requests for subsequent requests for that email within the window, regardless of whether the email is registered, and does not send any further reset emails

#### Scenario: Per-IP rate limit exceeded

- **WHEN** an unauthenticated caller submits more than the configured number of password-reset requests from the same client IP within the configured rolling window, across any combination of email addresses
- **THEN** the system returns 429 Too Many Requests for subsequent requests from that IP within the window

### Requirement: Password reset token is single-use and time-limited

The system SHALL accept a password-reset token at the reset endpoint only if the token is unexpired, has not previously been redeemed, and was issued for the account whose email is presented alongside it. A successful reset SHALL invalidate the token so it cannot be reused, and SHALL mark the account's email as confirmed, since the token was delivered to it.

#### Scenario: Successful reset

- **WHEN** an unauthenticated caller submits a valid, unexpired, never-redeemed reset token together with the matching email and a new password that satisfies the system's password policy
- **THEN** the system updates the user's password hash, invalidates the token, marks the email confirmed, returns 204 No Content, and does NOT issue any new access or refresh tokens to the caller

#### Scenario: Expired token rejected

- **WHEN** an unauthenticated caller submits a reset token that was issued more than the configured token lifespan ago
- **THEN** the system returns 400 Bad Request with a generic "invalid or expired token" error and does not modify the user's password

#### Scenario: Replayed token rejected

- **WHEN** an unauthenticated caller submits a reset token that has already been redeemed for a successful password reset or a passkey recovery
- **THEN** the system returns 400 Bad Request with a generic "invalid or expired token" error and does not modify the user's password

#### Scenario: Token presented with the wrong email rejected

- **WHEN** an unauthenticated caller submits a syntactically valid reset token together with an email that is not the one the token was issued for
- **THEN** the system returns 400 Bad Request with a generic "invalid or expired token" error and does not modify any user's password

#### Scenario: New password violates policy

- **WHEN** an unauthenticated caller submits a valid token plus a new password that does not satisfy the system's password policy (e.g. too short)
- **THEN** the system returns 400 Bad Request with a field-level validation error, the token remains unredeemed, and the user's existing password is unchanged

### Requirement: Successful password reset revokes existing sessions

The system SHALL revoke all of a user's existing refresh tokens as part of a successful password reset, so that any concurrent or pre-existing sessions for that user are forced to re-authenticate before they can obtain new access tokens.

#### Scenario: Concurrent sessions are kicked

- **WHEN** a user successfully resets their password while one or more refresh-token cookies issued before the reset are still outstanding
- **THEN** any subsequent call to the token-refresh endpoint that presents one of those outstanding refresh tokens is rejected with an HTTP error response and does not return a new access token (the exact status code follows the existing refresh-endpoint convention)

### Requirement: Successful password reset sends a confirmation email

The system SHALL send a confirmation email to the user's registered email address after a successful password reset, informing them that the password was just changed and providing a path to take action if they did not initiate the change. The confirmation email SHALL be sent best-effort: a failure to deliver this email MUST NOT cause the reset endpoint to return an error, because the password change has already succeeded and the user must be allowed to log in.

#### Scenario: Confirmation email is sent after a successful reset

- **WHEN** a user successfully resets their password through the reset endpoint
- **THEN** the system sends an email to the user's registered email address with a subject indicating their password was changed, a body that includes the approximate time of the change and instructions for what to do if they did not perform the change, and the reset endpoint still returns 204 No Content to the caller

#### Scenario: Confirmation email delivery failure does not fail the reset

- **WHEN** a user successfully resets their password but the confirmation email cannot be delivered (SMTP timeout, rejection, network failure)
- **THEN** the system logs the delivery failure, returns 204 No Content for the reset request, and leaves the user's new password in place — the user can sign in with the new password regardless of confirmation-email outcome

### Requirement: Reset link points at the SPA over HTTPS

The system SHALL build the reset link delivered in the email using the configured public SPA base URL, and that base URL SHALL be HTTPS. The system SHALL fail to start if the configured public base URL is not an `https://` URL in non-Development environments.

#### Scenario: Reset link uses HTTPS in production

- **WHEN** the application is configured with a production public base URL and sends a password-reset email
- **THEN** the link embedded in the email begins with `https://` and points at the SPA's `/reset-password` route, carrying the email and token as query parameters

#### Scenario: Misconfigured non-HTTPS base URL fails startup

- **WHEN** the application is started in a non-Development environment with `App:PublicBaseUrl` missing or set to a non-`https://` URL
- **THEN** the application fails to start and surfaces a configuration error, rather than silently sending reset links that the browser would refuse to treat as a secure origin

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

### Requirement: Passkey sign-in
The system SHALL let a user sign in with a passkey registered to their account, without entering an email address or password. A successful passkey sign-in SHALL produce the same session as a password sign-in: an access token in the response and a refresh-token cookie.

#### Scenario: Successful passkey sign-in
- **WHEN** a visitor requests passkey sign-in options and then submits a valid assertion from a passkey registered to an account
- **THEN** the system returns 200 OK with an access token and the user, sets the refresh-token cookie, and stores the passkey's new signature counter

#### Scenario: Unknown or invalid passkey rejected
- **WHEN** a visitor submits an assertion for a credential that is not registered, has an invalid signature, a wrong challenge, or an origin the system does not expect
- **THEN** the system returns 400 Bad Request with a generic error and issues no session

#### Scenario: Assertion without a matching ceremony rejected
- **WHEN** a visitor submits an assertion without the ceremony cookie from a sign-in options request, with an expired one, or with one issued for a different kind of ceremony
- **THEN** the system returns 400 Bad Request with a generic error and issues no session

### Requirement: Passkey ceremony state is server-protected and single-use
The system SHALL keep the state of every passkey ceremony (sign-in, sign-up, adding a passkey, recovery) out of reach of page scripts, protected against tampering, valid for at most 5 minutes, and usable for one completion attempt only.

#### Scenario: Ceremony cookie attributes
- **WHEN** the system returns passkey options for any ceremony
- **THEN** it sets a ceremony cookie that is HttpOnly, Secure, SameSite=Strict, scoped to the auth API path, and expires within 5 minutes

#### Scenario: Ceremony cookie cleared after completion
- **WHEN** a client completes a ceremony, successfully or not
- **THEN** the system clears the ceremony cookie, so submitting the same state again fails

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

### Requirement: Passkey management
The system SHALL let a signed-in user list, add, rename and remove their passkeys, and SHALL refuse to remove the last remaining way to sign in.

#### Scenario: View sign-in methods
- **WHEN** a signed-in user requests their security overview
- **THEN** the system returns whether a password is set, whether the email is confirmed, and each passkey's identifier, name, creation time and whether it is synced (backed up)

#### Scenario: Add a passkey
- **WHEN** a signed-in user requests options for a new passkey and submits a valid attestation
- **THEN** the system stores the passkey on their account with the given name and emails the account that a passkey was added

#### Scenario: Remove a passkey
- **WHEN** a signed-in user removes one of their passkeys while another passkey or a password remains
- **THEN** the system deletes it and returns 204 No Content

#### Scenario: Last sign-in method protected
- **WHEN** a signed-in user without a password tries to remove their only passkey
- **THEN** the system returns 409 Conflict and keeps the passkey

#### Scenario: Passkey of another account
- **WHEN** a signed-in user tries to rename or remove a passkey that is not on their account
- **THEN** the system returns 404 Not Found

### Requirement: Sign out other devices
The system SHALL let a signed-in user end every other session while keeping the current one.

#### Scenario: Other sessions revoked
- **WHEN** a signed-in user asks to sign out other devices
- **THEN** the system revokes every refresh token of the user except the one in the request's cookie

### Requirement: Email confirmation
The system SHALL send every new account a link to confirm its email address, SHALL let the user use the app before confirming, and SHALL let a signed-in user request the link again.

#### Scenario: Confirm with a valid link
- **WHEN** someone submits the user ID and token from an unexpired confirmation link
- **THEN** the system marks the account's email as confirmed and returns 204 No Content

#### Scenario: Invalid confirmation link
- **WHEN** someone submits an unknown user ID or an invalid or expired token
- **THEN** the system returns 400 Bad Request with a generic error

#### Scenario: Resend
- **WHEN** a signed-in user with an unconfirmed email asks for a new confirmation link
- **THEN** the system sends one and returns 202 Accepted

### Requirement: Recover access with a passkey
The system SHALL let a person holding a valid account-recovery link add a passkey on their current device and sign in. Doing so SHALL invalidate the link, confirm the email address and end every other session.

#### Scenario: Successful passkey recovery
- **WHEN** a visitor requests recovery passkey options with a valid, unexpired recovery token and its email, then submits a valid attestation
- **THEN** the system adds the passkey to that account, invalidates the token, marks the email confirmed, revokes all existing refresh tokens, returns an access token, and sets a new refresh-token cookie

#### Scenario: Invalid recovery token
- **WHEN** a visitor requests recovery options or completes recovery with an unknown email, or an invalid, expired or already-used token
- **THEN** the system returns 400 Bad Request with a generic "invalid or expired link" error and changes nothing

### Requirement: Password sign-in is rate-limited
The system SHALL limit failed password sign-ins per email address and per client IP address. After 10 failed attempts for one email address, or 30 from one IP address, within 15 minutes, the system SHALL refuse further password sign-ins for that email address or from that IP address with 429 Too Many Requests until the window ends, without checking the password. Unknown email addresses SHALL count like wrong passwords. Passkey sign-in and password reset SHALL NOT be affected.

#### Scenario: Too many wrong passwords for one account
- **WHEN** 10 password sign-ins for the same email address fail within 15 minutes
- **THEN** the next password sign-in for that email address returns 429, even with the correct password

#### Scenario: Too many failures from one IP address
- **WHEN** 30 password sign-ins from the same IP address fail within 15 minutes, spread over different email addresses
- **THEN** the next password sign-in from that IP address returns 429

#### Scenario: Normal use is unaffected
- **WHEN** a user mistypes their password a few times and then enters it correctly
- **THEN** they are signed in

#### Scenario: Other ways in keep working
- **WHEN** password sign-in is blocked for an email address
- **THEN** the user can still sign in with a passkey or request a password reset

### Requirement: Sign-in redirect stays on the site
After signing in, the SPA SHALL follow the login page's `redirect` parameter only when it is a path on the same site. A value with a backslash, a protocol-relative `//` start, a scheme, or anything else that resolves to another origin SHALL be ignored, and the SPA SHALL go to the home page instead.

#### Scenario: Redirect within the site
- **WHEN** a user signs in from `/login?redirect=/circles/123`
- **THEN** the SPA opens `/circles/123`

#### Scenario: Redirect to another site is ignored
- **WHEN** a user signs in from `/login?redirect=/\evil.example` (or `//evil.example`, or `https://evil.example`)
- **THEN** the SPA opens the home page and never leaves the site

### Requirement: Account fields have length limits
Every sign-up path (password and passkey) SHALL refuse with 400 Bad Request, before creating anything, a display name longer than 100 characters, an email address longer than 256 characters, or a password longer than 256 characters. The SPA's inputs SHALL not accept more than these limits.

#### Scenario: Overlong display name at sign-up
- **WHEN** someone signs up with a 101-character display name
- **THEN** the system returns 400 with a message naming the limit, and no account is created

#### Scenario: Overlong email at sign-up
- **WHEN** someone signs up with an email address longer than 256 characters
- **THEN** the system returns 400, not 500

### Requirement: Passkeys belong to kadoozle.app
Passkeys SHALL be created for and checked against the relying party `kadoozle.app`, so they work on `www.kadoozle.app` and `kadoozle.app`. Passkeys made for `giftcircles.app` before the move can no longer sign in; they SHALL be removed at the move, so they no longer show on the security page or count as a way to sign in. Accounts that had only passkeys SHALL get back in through the existing "Can't sign in?" email link and add a new passkey there.

#### Scenario: New passkey works on the new address
- **WHEN** a user adds a passkey on `https://www.kadoozle.app` and later signs in with it
- **THEN** sign-in succeeds

#### Scenario: Old passkeys are cleared
- **WHEN** the move has been deployed
- **THEN** no user has a stored passkey made for `giftcircles.app`, and users without a password and without a passkey can still recover access by email

#### Scenario: Prompted to add a passkey again
- **WHEN** a user whose passkeys were cleared signs in with their password
- **THEN** the app offers to add a passkey, as it does for any account without one

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

