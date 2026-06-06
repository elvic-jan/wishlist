# gift-claiming Specification

## Purpose
TBD - created by archiving change family-gift-wishlist. Update Purpose after archive.
## Requirements
### Requirement: Reserve a gift
The system SHALL allow an authenticated group member to mark an unclaimed wishlist item as reserved, signalling their intention to purchase it.

#### Scenario: Successful reservation
- **WHEN** an authenticated user marks an unclaimed wishlist item (owned by another member in a shared group) as reserved
- **THEN** the system creates a gift claim with status "Reserved" linked to that user, and returns the updated item with claim status

#### Scenario: Item already claimed
- **WHEN** an authenticated user attempts to reserve an item that already has an active claim (Reserved or Bought) by another user
- **THEN** the system returns a 409 Conflict error and does not alter the existing claim

#### Scenario: Owner cannot claim their own item
- **WHEN** an authenticated user attempts to reserve or mark as bought an item from their own wishlist
- **THEN** the system returns a 403 Forbidden error

#### Scenario: Non-group-member cannot claim
- **WHEN** an authenticated user who shares no group with the wishlist owner attempts to reserve an item
- **THEN** the system returns a 403 Forbidden error

### Requirement: Mark a gift as bought
The system SHALL allow the claimer of a reserved gift to update its status to "Bought".

#### Scenario: Successful status update to bought
- **WHEN** the authenticated user who holds the reservation for an item updates its status to "Bought"
- **THEN** the system updates the claim status to "Bought" and returns the updated item

#### Scenario: Non-claimer cannot mark as bought
- **WHEN** an authenticated user who is not the current claimer attempts to update a claimed item to "Bought"
- **THEN** the system returns a 403 Forbidden error

### Requirement: Release a claim
The system SHALL allow the claimer of a gift to release their claim, returning the item to unclaimed status.

#### Scenario: Successful release
- **WHEN** the authenticated user who holds the claim on an item (Reserved or Bought) releases it
- **THEN** the system removes the gift claim record and the item returns to unclaimed status

#### Scenario: Non-claimer cannot release
- **WHEN** an authenticated user who does not hold the claim on an item attempts to release it
- **THEN** the system returns a 403 Forbidden error

### Requirement: Claim status hidden from owner
The system SHALL ensure that a wishlist owner never receives claim status information for items on their own wishlist through any API response.

#### Scenario: Owner's wishlist response contains no claim data
- **WHEN** an authenticated user requests their own wishlist via any API endpoint
- **THEN** the system returns item data with no claim status, claimer identity, or reservation information included in the response payload

#### Scenario: Claim status visible to other group members
- **WHEN** an authenticated user who shares a group with the wishlist owner requests that owner's wishlist
- **THEN** the system returns each item annotated with its current claim status (unclaimed / reserved / bought) and whether the requesting user is the claimer

