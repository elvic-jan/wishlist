## ADDED Requirements

### Requirement: Password reset request endpoint is enumeration-safe

The system SHALL expose a public endpoint that accepts an email address and triggers delivery of a password-reset email if (and only if) the email belongs to a registered user. The endpoint's response SHALL NOT differ between the "known email" and "unknown email" cases in either status code, response body, or response timing characteristics, so that an unauthenticated caller cannot use it to enumerate registered users.

#### Scenario: Request for a registered email

- **WHEN** an unauthenticated caller submits a syntactically valid email address that belongs to a registered user to the password-reset request endpoint
- **THEN** the system generates a single-use password-reset token bound to that user, sends an email to the user containing a reset link carrying the token, and returns 202 Accepted with an empty body

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

The system SHALL accept a password-reset token at the reset endpoint only if the token is unexpired, has not previously been redeemed, and was issued for the account whose email is presented alongside it. A successful reset SHALL invalidate the token so it cannot be reused.

#### Scenario: Successful reset

- **WHEN** an unauthenticated caller submits a valid, unexpired, never-redeemed reset token together with the matching email and a new password that satisfies the system's password policy
- **THEN** the system updates the user's password hash, invalidates the token, returns 204 No Content, and does NOT issue any new access or refresh tokens to the caller

#### Scenario: Expired token rejected

- **WHEN** an unauthenticated caller submits a reset token that was issued more than the configured token lifespan ago
- **THEN** the system returns 400 Bad Request with a generic "invalid or expired token" error and does not modify the user's password

#### Scenario: Replayed token rejected

- **WHEN** an unauthenticated caller submits a reset token that has already been redeemed for a successful password reset
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
