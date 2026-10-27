# wishlists Specification

## Purpose
TBD - created by archiving change family-gift-wishlist. Update Purpose after archive.
## Requirements
### Requirement: Add wishlist item
The system SHALL allow an authenticated user to add a gift item to their personal wishlist. An item carries a required title and optional description, plus an optional ordered list of up to 10 URLs.

#### Scenario: Successful item addition

- **WHEN** an authenticated user submits a title for a new wishlist item (with optional description, and optionally up to 10 ordered URLs)
- **THEN** the system creates the item on their wishlist, persists the URLs in the submitted order, and returns the created item including the URL list

#### Scenario: Missing title rejected

- **WHEN** an authenticated user submits a wishlist item with an empty or missing title
- **THEN** the system returns a 400 Bad Request and does not create the item

#### Scenario: More than 10 URLs rejected

- **WHEN** an authenticated user submits a wishlist item with 11 or more URLs
- **THEN** the system returns a 400 Bad Request and does not create the item

#### Scenario: Malformed URL rejected

- **WHEN** an authenticated user submits a wishlist item where any URL in the list is not an absolute `http://` or `https://` URL (for example a relative path, a `javascript:` URI, or whitespace-only)
- **THEN** the system returns a 400 Bad Request and does not create the item

### Requirement: View own wishlist
The system SHALL allow an authenticated user to view their own wishlist items without any claim status information. Each item SHALL say who added it when that was a buddy.

#### Scenario: Own wishlist returned without claim status

- **WHEN** an authenticated user requests their own wishlist
- **THEN** the system returns all their wishlist items with title, description, and the ordered URL list — but with no reservation or purchase status attached

#### Scenario: Buddy-added wish shows who added it, without claim status

- **WHEN** an authenticated user requests their own wishlist and an item on it was added by a buddy and has been reserved
- **THEN** the item includes the buddy's id and display name as "added by", and still has no reservation or purchase status attached

### Requirement: Edit wishlist item
The system SHALL allow an authenticated user to update the title, description, or URL list of an item they own. Editing the URL list replaces it wholesale with the submitted list (preserving the new order). The owner's buddies can also edit the owner's items (see `wishlist-buddies`); on the owner's own endpoint, items of other users are still rejected.

#### Scenario: Successful edit

- **WHEN** an authenticated user submits updated fields for a wishlist item they own, including optionally a new URL list
- **THEN** the system updates the item, replaces any prior URLs with the newly submitted list in the submitted order, and returns the updated item

#### Scenario: Editing another user's item rejected

- **WHEN** an authenticated user attempts to edit a wishlist item they do not own and they are not that owner's buddy with a shared circle
- **THEN** the system returns a 403 Forbidden error

#### Scenario: Edit with more than 10 URLs rejected

- **WHEN** an authenticated user submits an edit for a wishlist item they own where the URL list contains 11 or more URLs
- **THEN** the system returns a 400 Bad Request and does not modify the item

#### Scenario: Edit with malformed URL rejected

- **WHEN** an authenticated user submits an edit where any URL in the list is not an absolute `http://` or `https://` URL
- **THEN** the system returns a 400 Bad Request and does not modify the item

### Requirement: Delete wishlist item
The system SHALL allow an authenticated user to remove an item from their own wishlist. The owner's buddies can also delete the owner's items (see `wishlist-buddies`).

#### Scenario: Successful deletion
- **WHEN** an authenticated user requests deletion of an item they own
- **THEN** the system removes the item and any associated gift claim, and returns 204 No Content

#### Scenario: Deleting another user's item rejected
- **WHEN** an authenticated user attempts to delete a wishlist item they do not own and they are not that owner's buddy with a shared circle
- **THEN** the system returns a 403 Forbidden error

### Requirement: View another member's wishlist
The system SHALL allow an authenticated group member to view the wishlist of another member they share a group with, including claim status. Each item SHALL say who added it when that was a buddy.

#### Scenario: Buyer views another member's wishlist with claim status
- **WHEN** an authenticated user requests the wishlist of another user who shares at least one group with them
- **THEN** the system returns all wishlist items for that user, each annotated with the current claim status (unclaimed, reserved, or bought) and whether the requesting user is the claimer

#### Scenario: Buddy-added wish shows who added it
- **WHEN** an authenticated user views another member's wishlist containing an item a buddy added
- **THEN** that item includes the buddy's id and display name as "added by"; items the owner added have no "added by"

#### Scenario: Access denied for non-shared-group user
- **WHEN** an authenticated user requests the wishlist of a user with whom they share no group
- **THEN** the system returns a 403 Forbidden error

### Requirement: List wishlists in a group
The system SHALL allow a group member to see a list of all other members' wishlists within a group (excluding their own).

#### Scenario: Group wishlists listed
- **WHEN** an authenticated group member requests the wishlists overview for a group they belong to
- **THEN** the system returns each other member's display name and a link to their wishlist (excluding the requesting user's own wishlist from the list)

