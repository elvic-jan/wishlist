## MODIFIED Requirements

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

#### Scenario: Two people reserve at the same moment
- **WHEN** two members reserve the same unclaimed item at the same time
- **THEN** exactly one of them gets the reservation, and the other gets a 409 Conflict saying someone else just reserved it (never a server error)
