## ADDED Requirements

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
