# groups Specification

## Purpose
TBD - created by archiving change family-gift-wishlist. Update Purpose after archive.
## Requirements
### Requirement: Create group
The system SHALL allow an authenticated user to create a new named group, becoming its owner.

#### Scenario: Successful group creation
- **WHEN** an authenticated user submits a non-empty group name
- **THEN** the system creates the group, adds the creator as an owner member, and returns the group details

#### Scenario: Empty name rejected
- **WHEN** an authenticated user submits an empty or whitespace-only group name
- **THEN** the system returns a 400 Bad Request and does not create the group

### Requirement: Generate invitation link
The system SHALL allow a group member to generate a time-limited invitation link for others to join the group.

#### Scenario: Invitation link generated
- **WHEN** an authenticated group member requests an invitation link for a group they belong to
- **THEN** the system creates an invitation with a unique code and an expiry of 7 days, and returns the invite URL

#### Scenario: Non-member cannot generate invitation
- **WHEN** an authenticated user who is not a member of the group requests an invitation link
- **THEN** the system returns a 403 Forbidden error

### Requirement: Join group via invitation
The system SHALL allow an authenticated user to join a group by following a valid invitation link, and SHALL allow a visitor without an account to join the group by registering with the invitation code.

#### Scenario: Successful join
- **WHEN** an authenticated user presents a valid, unexpired invitation code for a group they are not already in
- **THEN** the system adds the user as a member of the group and marks the invitation as used

#### Scenario: Join while registering
- **WHEN** a visitor without an account registers with a valid, unexpired invitation code
- **THEN** the system adds the newly created user as a member of the invitation's group and marks the invitation as used, as part of the same registration request

#### Scenario: Invitation remains usable by others within its validity window
- **WHEN** an invitation that has already been used (to join or to register) is presented again by a different person before its expiry date
- **THEN** the system accepts it in the same way as an unused invitation

#### Scenario: Already a member
- **WHEN** an authenticated user presents a valid invitation code for a group they are already in
- **THEN** the system returns a 200 OK without adding a duplicate membership

#### Scenario: Expired invitation rejected
- **WHEN** an authenticated user presents an invitation code past its expiry date
- **THEN** the system returns a 410 Gone error and does not add the user to the group

#### Scenario: Invalid invitation code rejected
- **WHEN** an authenticated user presents an invitation code that does not exist
- **THEN** the system returns a 404 Not Found error

### Requirement: List group members
The system SHALL allow a group member to see all other members of their group.

#### Scenario: Members listed
- **WHEN** an authenticated group member requests the member list for a group they belong to
- **THEN** the system returns the display names and user IDs of all members in the group

#### Scenario: Non-member access denied
- **WHEN** an authenticated user who is not a member requests the member list
- **THEN** the system returns a 403 Forbidden error

### Requirement: Leave group
The system SHALL allow a group member to remove themselves from a group.

#### Scenario: Successful leave
- **WHEN** an authenticated non-owner member requests to leave a group
- **THEN** the system removes the membership and they lose access to that group's wishlists

#### Scenario: Owner cannot leave while members remain
- **WHEN** the group owner attempts to leave while other members are still in the group
- **THEN** the system returns a 400 Bad Request indicating ownership must be transferred or the group deleted first

### Requirement: Check an invitation without signing in
The system SHALL expose a public, unauthenticated endpoint that reports whether an invitation code can currently be used, so a visitor can be told before they fill in the registration form. Checking a code SHALL NOT mark the invitation as used.

#### Scenario: Valid invitation
- **WHEN** a client checks an existing, unexpired invitation code
- **THEN** the system returns 200 OK with the invitation's expiry time and does not mark the invitation as used

#### Scenario: Unknown invitation
- **WHEN** a client checks an invitation code that does not exist or is not in the system's code format
- **THEN** the system returns 404 Not Found

#### Scenario: Expired invitation
- **WHEN** a client checks an invitation code past its expiry date
- **THEN** the system returns 410 Gone

### Requirement: Invitation code guessing is rate-limited
The system SHALL limit the number of wrong invitation codes a single client IP can submit within a rolling time window, counted across registration-with-invitation, join-via-invitation, and the invitation check. Once the limit is exceeded, the system SHALL refuse every invitation-code attempt from that IP with 429 Too Many Requests until the window passes, without evaluating the submitted code, so that a correct guess is never confirmed while blocked.

#### Scenario: Wrong codes within the limit
- **WHEN** a client IP submits unknown invitation codes fewer times than the configured limit within the window
- **THEN** each attempt returns the normal 404 Not Found response

#### Scenario: Limit exceeded blocks further attempts
- **WHEN** a client IP has submitted the configured number of unknown invitation codes within the window and submits another invitation code, whether to the registration, join, or check endpoint
- **THEN** the system returns 429 Too Many Requests with a generic message, does not look up the code, and does not create an account or membership

#### Scenario: Correct code refused while blocked
- **WHEN** a blocked client IP submits a valid, unexpired invitation code before the window has passed
- **THEN** the system returns 429 Too Many Requests and neither creates an account nor adds a membership

#### Scenario: Block lifts after the window
- **WHEN** the window has passed since a client IP was blocked
- **THEN** that IP can again present invitation codes and a valid code is accepted normally

#### Scenario: Malformed codes count as wrong guesses
- **WHEN** a client submits an invitation code that is not in the system's code format
- **THEN** the system returns 404 Not Found without a database lookup and counts the attempt toward the IP's limit

#### Scenario: Expired codes do not count
- **WHEN** a client submits an invitation code that exists but has expired
- **THEN** the system returns 410 Gone and does not count the attempt toward the IP's limit

#### Scenario: Limits are per IP
- **WHEN** one client IP is blocked and a different client IP presents a valid invitation code
- **THEN** the other IP's attempt is accepted normally

