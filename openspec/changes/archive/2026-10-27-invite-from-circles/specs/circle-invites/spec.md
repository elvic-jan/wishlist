## ADDED Requirements

### Requirement: Invite candidates
The system SHALL let a member of a circle list the people they can invite into it, grouped by their other circles. Each group SHALL hold the members of one of the caller's other circles, excluding the caller and anyone already in the target circle, and SHALL mark people who already have an open invitation to the target circle. Circles with nobody left SHALL be left out.

#### Scenario: Candidates grouped by circle
- **WHEN** a member of circle A requests invite candidates for A, and they are also in circles B and C
- **THEN** the system returns B and C, each with its members who are not in A and not the caller, and marks those already invited to A

#### Scenario: Person in several circles
- **WHEN** a candidate is in both B and C
- **THEN** they appear in both groups

#### Scenario: Non-member denied
- **WHEN** a user who is not a member of circle A requests invite candidates for A
- **THEN** the system returns 403 Forbidden

### Requirement: Invite people from my circles
The system SHALL allow a member of a circle to invite one or more users who share at least one circle with them. Each newly invited user SHALL get an open invitation to the circle and an email naming the inviter and the circle. People who are already members or already invited SHALL be skipped without a second invitation or email.

#### Scenario: Several people invited
- **WHEN** a member of circle A invites three users who each share a circle with them and are not in A
- **THEN** the system creates an open invitation for each, emails each of them, and returns 3 invited and 0 skipped

#### Scenario: Already member or already invited
- **WHEN** the list includes someone already in A and someone already invited to A
- **THEN** the system skips both, creating no invitation and sending no email for them

#### Scenario: Not sharing a circle
- **WHEN** the list includes a user who shares no circle with the inviter
- **THEN** that user is skipped; if nobody in the list may be invited, the system returns 403 Forbidden

#### Scenario: Non-member denied
- **WHEN** a user who is not a member of circle A tries to invite people into A
- **THEN** the system returns 403 Forbidden and creates nothing

#### Scenario: Too many at once
- **WHEN** a request lists more than 100 users, or none
- **THEN** the system returns 400 Bad Request and creates nothing

#### Scenario: Email failure does not block
- **WHEN** the invitation email cannot be sent
- **THEN** the invitation is still created

### Requirement: My invitations
The system SHALL let a user list their open invitations, each with the circle's name and the inviter's name.

#### Scenario: Invitations listed
- **WHEN** a user with open invitations requests them
- **THEN** the system returns each with its id, circle id, circle name, inviter name and date, newest first

### Requirement: Join or decline an invitation
The system SHALL let the invited user join or decline their invitation. Joining SHALL make them a member of the circle. Either answer SHALL close the invitation, and a declined user MAY be invited again.

#### Scenario: Join
- **WHEN** the invited user joins
- **THEN** the system adds them to the circle as a member and closes the invitation

#### Scenario: Join when already a member
- **WHEN** the invited user joins a circle they meanwhile joined by link
- **THEN** the system closes the invitation without adding a duplicate membership

#### Scenario: Decline
- **WHEN** the invited user declines
- **THEN** the system closes the invitation without adding them, and the inviter is not notified

#### Scenario: Someone else's invitation
- **WHEN** a user tries to join or decline an invitation addressed to someone else, or one that no longer exists
- **THEN** the system returns 404 Not Found and changes nothing

#### Scenario: Circle deleted
- **WHEN** the circle of an open invitation is deleted
- **THEN** the invitation disappears with it
