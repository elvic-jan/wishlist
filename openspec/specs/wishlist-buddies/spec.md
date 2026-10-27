# wishlist-buddies Specification

## Purpose
Lets a wishlist owner pick buddies among people they share a circle with; a buddy has full control over the owner's list (add, edit, delete any wish) while reservations stay hidden from the owner.
## Requirements
### Requirement: Add a buddy
The system SHALL allow an authenticated user (the owner) to make another user a buddy for their wishlist, provided the two share at least one circle. The system SHALL send the new buddy an email that names the owner and links to the owner's wishlist.

#### Scenario: Buddy added
- **WHEN** an owner adds a user they share a circle with as a buddy
- **THEN** the system records the buddy, sends the buddy a notification email, and returns 200 with the buddy's id and display name

#### Scenario: No shared circle rejected
- **WHEN** an owner tries to add a user they share no circle with
- **THEN** the system returns 403 Forbidden and records nothing

#### Scenario: Adding yourself rejected
- **WHEN** an owner tries to add themselves as a buddy
- **THEN** the system returns 400 Bad Request

#### Scenario: Already a buddy
- **WHEN** an owner adds a user who is already their buddy
- **THEN** the system returns 200 without creating a duplicate and without sending another email

#### Scenario: Email failure does not block
- **WHEN** the notification email cannot be sent
- **THEN** the buddy is still recorded and the request succeeds

### Requirement: Remove a buddy
The system SHALL allow an owner to remove a buddy. Wishes that buddy already added SHALL stay on the owner's list.

#### Scenario: Buddy removed
- **WHEN** an owner removes one of their buddies
- **THEN** the system deletes the buddy relationship, returns 204, and the buddy's earlier wishes remain with their "added by" label

#### Scenario: Removing a non-buddy
- **WHEN** an owner removes a user who is not their buddy
- **THEN** the system returns 204 and changes nothing

### Requirement: List buddies and candidates
The system SHALL let an owner list their current buddies and the users they could add as buddies. Buddies who no longer share a circle with the owner SHALL be left out of the buddies list. Candidates SHALL be the users who share at least one circle with the owner, excluding the owner and their current buddies.

#### Scenario: Buddies listed
- **WHEN** an owner requests their buddies
- **THEN** the system returns each buddy who still shares a circle with them, with id and display name

#### Scenario: Candidates listed
- **WHEN** an owner requests buddy candidates
- **THEN** the system returns every user who shares a circle with them, once each, excluding themselves and existing buddies

### Requirement: List wishlists I am a buddy for
The system SHALL let a user list the owners whose wishlists they can currently add to (buddy relationship and a shared circle).

#### Scenario: Buddy-of listed
- **WHEN** a user requests the lists they are a buddy for
- **THEN** the system returns each such owner's id and display name

### Requirement: Buddy adds a wish
The system SHALL allow a buddy to add a wish to the owner's wishlist while they share at least one circle. The wish SHALL belong to the owner, record the buddy as the one who added it, be visible right away, and follow the same validation as adding your own wish (required title, up to 10 absolute http/https URLs).

#### Scenario: Wish added by buddy
- **WHEN** a buddy who shares a circle with the owner submits a valid wish for the owner
- **THEN** the system creates the wish on the owner's list with the buddy as the adder and returns it, including the "added by" information

#### Scenario: Non-buddy rejected
- **WHEN** a user who is not the owner's buddy submits a wish for the owner
- **THEN** the system returns 403 Forbidden and creates nothing

#### Scenario: Buddy without shared circle rejected
- **WHEN** a buddy who no longer shares any circle with the owner submits a wish for the owner
- **THEN** the system returns 403 Forbidden and creates nothing

#### Scenario: Invalid wish rejected
- **WHEN** a buddy submits a wish with an empty title, more than 10 URLs, or a malformed URL
- **THEN** the system returns 400 Bad Request and creates nothing

### Requirement: Buddy edits or deletes any wish
The system SHALL allow a buddy, while they share at least one circle with the owner, to edit or delete any wish on the owner's list, including wishes the owner added. Edits SHALL follow the same validation and effects as the owner editing their own wish. Deleting SHALL also remove any claim on the wish, the same as when the owner deletes it. The owner SHALL keep full control over all wishes, including buddy-added ones.

#### Scenario: Buddy edits an owner-added wish
- **WHEN** a buddy who shares a circle with the owner submits valid changes for a wish the owner added
- **THEN** the system updates the wish and returns it; its "added by" stays unchanged

#### Scenario: Buddy deletes a wish
- **WHEN** a buddy who shares a circle with the owner deletes a wish on the owner's list
- **THEN** the system removes the wish and any claim on it, and returns 204 No Content

#### Scenario: Non-buddy edit or delete rejected
- **WHEN** a user who is not the owner's buddy, or a buddy who no longer shares a circle with the owner, tries to edit or delete a wish on the owner's list
- **THEN** the system returns 403 Forbidden and changes nothing

#### Scenario: Wish not on that owner's list
- **WHEN** a buddy edits or deletes a wish id that does not belong to the given owner
- **THEN** the system returns 404 Not Found and changes nothing

#### Scenario: Invalid edit rejected
- **WHEN** a buddy submits an edit with more than 10 URLs or a malformed URL
- **THEN** the system returns 400 Bad Request and does not modify the wish

#### Scenario: Owner edits a buddy-added wish
- **WHEN** the owner edits or deletes a wish a buddy added
- **THEN** the system applies the change like for any of the owner's own wishes

