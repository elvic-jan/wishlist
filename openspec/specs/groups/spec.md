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
The system SHALL allow a group member to generate a time-limited invitation link for others to join the group. Each request SHALL create a new link that several people can use until it expires.

#### Scenario: Invitation link generated
- **WHEN** an authenticated group member requests an invitation link for a group they belong to
- **THEN** the system creates an invitation with a unique code and an expiry of 3 days, and returns the invite URL

#### Scenario: Links already sent keep their expiry
- **WHEN** an invitation created before the expiry changed to 3 days is used within its original 7 days
- **THEN** the system accepts it

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
The system SHALL expose a public, unauthenticated endpoint that reports whether an invitation code can currently be used, and who invited the visitor to which circle, so a visitor can be told before they fill in the registration form. Checking a code SHALL NOT mark the invitation as used.

#### Scenario: Valid invitation
- **WHEN** a client checks an existing, unexpired invitation code
- **THEN** the system returns 200 OK with the invitation's expiry time, the display name of the member who created it, and the circle's name, and does not mark the invitation as used

#### Scenario: Inviter no longer exists
- **WHEN** a client checks a valid invitation whose creator has deleted their account
- **THEN** the inviter's name is returned as "Someone"

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

### Requirement: Share an invitation
The circle page SHALL let a member create an invitation and share it through the device's share sheet (where the browser supports it), WhatsApp, the member's own email app, or by copying the link. Every option SHALL carry the same message, naming the inviter, the circle and how long the link works, followed by the link.

#### Scenario: Share sheet
- **WHEN** a member on a browser that supports sharing chooses "Share…"
- **THEN** the device's share sheet opens with the invitation message and link

#### Scenario: No share sheet
- **WHEN** the browser can't share natively
- **THEN** "Share…" is not offered, and WhatsApp, Email and Copy link still are

#### Scenario: WhatsApp
- **WHEN** a member chooses "WhatsApp"
- **THEN** WhatsApp (app or web) opens with the invitation message and link filled in

#### Scenario: Email
- **WHEN** a member chooses "Email"
- **THEN** their mail app opens a new message with a subject naming the circle and the invitation message and link in the body, and GiftCircles sends no email itself

#### Scenario: Copy link
- **WHEN** a member chooses "Copy link"
- **THEN** the invite link is copied to the clipboard

#### Scenario: New link each time
- **WHEN** a member opens "Invite people" twice
- **THEN** two different links are created, and both work for anyone until they expire

### Requirement: Invite link preview
When an invite link is shared in a messaging app, the page it points to SHALL carry link-preview tags naming the inviter and the circle, with the GiftCircles preview image. Any other page, and any invite code that can't be used or is rate-limited, SHALL carry generic GiftCircles preview tags, so a preview never confirms a guessed code.

#### Scenario: Personal preview
- **WHEN** a link-preview crawler fetches a valid, unexpired invite link
- **THEN** the page's preview title is "<inviter> invited you to <circle> on GiftCircles", with the GiftCircles preview image at an absolute URL

#### Scenario: Generic preview for unusable codes
- **WHEN** a crawler fetches an invite link that is unknown, expired, or comes from a client that is rate-limited for wrong codes
- **THEN** the page carries the generic GiftCircles preview and reveals no circle or inviter

#### Scenario: Page still works
- **WHEN** a person opens an invite link in a browser
- **THEN** they get the normal app, which shows "<inviter> invited you to <circle>" on the join page

