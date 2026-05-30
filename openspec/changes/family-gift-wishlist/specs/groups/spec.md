## ADDED Requirements

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
The system SHALL allow an authenticated user to join a group by following a valid invitation link.

#### Scenario: Successful join
- **WHEN** an authenticated user presents a valid, unexpired invitation code for a group they are not already in
- **THEN** the system adds the user as a member of the group and marks the invitation as used

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
