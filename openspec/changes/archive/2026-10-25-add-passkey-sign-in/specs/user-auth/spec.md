## ADDED Requirements

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
The system SHALL let a visitor create an account with a passkey instead of a password, under the same rules as password registration: a valid invitation is required (except for the very first account), the email must not already be registered, and the account joins the invitation's circle in the same operation.

#### Scenario: Successful passkey sign-up with an invitation
- **WHEN** a visitor requests passkey sign-up options with a valid invitation code, an unused email and a display name, then submits a valid attestation with the same email and display name
- **THEN** the system creates an account without a password, stores the passkey, adds the account to the invitation's circle, marks the invitation used, returns an access token with the joined circle, sets the refresh-token cookie, and sends an email-confirmation message

#### Scenario: Invitation gate applies to passkey sign-up
- **WHEN** a visitor requests passkey sign-up options or submits a passkey sign-up without a valid invitation while accounts exist, or with an unknown or expired code
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

## MODIFIED Requirements

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
