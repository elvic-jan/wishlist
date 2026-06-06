## ADDED Requirements

### Requirement: Add wishlist item
The system SHALL allow an authenticated user to add a gift item to their personal wishlist.

#### Scenario: Successful item addition
- **WHEN** an authenticated user submits a title for a new wishlist item (with optional description, URL, and price)
- **THEN** the system creates the item on their wishlist and returns the created item

#### Scenario: Missing title rejected
- **WHEN** an authenticated user submits a wishlist item with an empty or missing title
- **THEN** the system returns a 400 Bad Request and does not create the item

### Requirement: View own wishlist
The system SHALL allow an authenticated user to view their own wishlist items without any claim status information.

#### Scenario: Own wishlist returned without claim status
- **WHEN** an authenticated user requests their own wishlist
- **THEN** the system returns all their wishlist items with title, description, URL, and price — but with no reservation or purchase status attached

### Requirement: Edit wishlist item
The system SHALL allow an authenticated user to update the title, description, URL, or price of an item they own.

#### Scenario: Successful edit
- **WHEN** an authenticated user submits updated fields for a wishlist item they own
- **THEN** the system updates the item and returns the updated item

#### Scenario: Editing another user's item rejected
- **WHEN** an authenticated user attempts to edit a wishlist item they do not own
- **THEN** the system returns a 403 Forbidden error

### Requirement: Delete wishlist item
The system SHALL allow an authenticated user to remove an item from their own wishlist.

#### Scenario: Successful deletion
- **WHEN** an authenticated user requests deletion of an item they own
- **THEN** the system removes the item and any associated gift claim, and returns 204 No Content

#### Scenario: Deleting another user's item rejected
- **WHEN** an authenticated user attempts to delete a wishlist item they do not own
- **THEN** the system returns a 403 Forbidden error

### Requirement: View another member's wishlist
The system SHALL allow an authenticated group member to view the wishlist of another member they share a group with, including claim status.

#### Scenario: Buyer views another member's wishlist with claim status
- **WHEN** an authenticated user requests the wishlist of another user who shares at least one group with them
- **THEN** the system returns all wishlist items for that user, each annotated with the current claim status (unclaimed, reserved, or bought) and whether the requesting user is the claimer

#### Scenario: Access denied for non-shared-group user
- **WHEN** an authenticated user requests the wishlist of a user with whom they share no group
- **THEN** the system returns a 403 Forbidden error

### Requirement: List wishlists in a group
The system SHALL allow a group member to see a list of all other members' wishlists within a group (excluding their own).

#### Scenario: Group wishlists listed
- **WHEN** an authenticated group member requests the wishlists overview for a group they belong to
- **THEN** the system returns each other member's display name and a link to their wishlist (excluding the requesting user's own wishlist from the list)
