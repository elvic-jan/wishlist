## MODIFIED Requirements

### Requirement: Create group
The system SHALL allow an authenticated user to create a new named group, becoming its owner. The name SHALL be at most 200 characters.

#### Scenario: Successful group creation
- **WHEN** an authenticated user submits a non-empty group name
- **THEN** the system creates the group, adds the creator as an owner member, and returns the group details

#### Scenario: Empty name rejected
- **WHEN** an authenticated user submits an empty or whitespace-only group name
- **THEN** the system returns a 400 Bad Request and does not create the group

#### Scenario: Overlong name rejected
- **WHEN** an authenticated user submits a group name longer than 200 characters
- **THEN** the system returns a 400 Bad Request, not 500, and does not create the group

### Requirement: Generate invitation link
The system SHALL allow a group member to generate a time-limited invitation link for others to join the group. Each request SHALL create a new link that several people can use until it expires. The link SHALL start with the configured public address (`App:PublicBaseUrl`), never with the address the request came in on.

#### Scenario: Invitation link generated
- **WHEN** an authenticated group member requests an invitation link for a group they belong to
- **THEN** the system creates an invitation with a unique code and an expiry of 3 days, and returns the invite URL

#### Scenario: Links already sent keep their expiry
- **WHEN** an invitation created before the expiry changed to 3 days is used within its original 7 days
- **THEN** the system accepts it

#### Scenario: Non-member cannot generate invitation
- **WHEN** an authenticated user who is not a member of the group requests an invitation link
- **THEN** the system returns a 403 Forbidden error

#### Scenario: Forged Host header
- **WHEN** a member requests an invitation link with the request's Host header set to another site
- **THEN** the returned link still starts with the configured public address
