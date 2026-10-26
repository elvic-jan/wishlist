## MODIFIED Requirements

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

## ADDED Requirements

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
