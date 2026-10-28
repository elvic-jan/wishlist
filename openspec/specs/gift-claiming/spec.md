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

#### Scenario: Two people reserve at the same moment
- **WHEN** two members reserve the same unclaimed item at the same time
- **THEN** exactly one of them gets the reservation, and the other gets a 409 Conflict saying someone else just reserved it (never a server error)

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

### Requirement: Reservations expire
A reservation with status "Reserved" SHALL end at the first monthly run that is at least 6 months after it was made or last kept. From then on, the system SHALL treat the item as unclaimed. A claim with status "Bought" SHALL never expire.

#### Scenario: Reservation runs out
- **WHEN** a reservation's end date has passed and it was never kept
- **THEN** the item is shown as unclaimed and any group member can reserve it

#### Scenario: End date falls on a monthly run
- **WHEN** a member reserves an item on 2 January
- **THEN** the reservation ends on 1 August (6 months later, rounded up to the next monthly run)

#### Scenario: Reserving an item whose reservation ran out
- **WHEN** a member reserves an item whose previous reservation has expired
- **THEN** the system replaces the expired claim with a new reservation for that member, instead of returning 409 Conflict

#### Scenario: Bought never expires
- **WHEN** a claim has been marked "Bought"
- **THEN** it stays in place no matter how long ago it was made

### Requirement: Reminder before a reservation expires
The system SHALL email the reserver once, one month before their reservation ends, saying the reservation is still open and linking to the wishlist, where they can keep it, mark it as bought, or release it.

#### Scenario: Reminder a month before the end
- **WHEN** the monthly run happens and a reservation ends at the next run, and no reminder has been sent for its current period
- **THEN** the system emails the reserver once, naming the item and whose wishlist it is on, with a link to that wishlist

#### Scenario: No reminder for bought or released gifts
- **WHEN** a claim was marked "Bought" or released before the reminder was due
- **THEN** no reminder is sent

### Requirement: Email when a reservation has expired
When a reservation ends, the system SHALL email the reserver that it ran out and that the item is free again for others, with a link to the wishlist in case they still want to reserve it.

#### Scenario: Expiry email
- **WHEN** the monthly run ends a reservation
- **THEN** the reserver gets one email naming the item and whose wishlist it is on, and the claim is removed

#### Scenario: Failed email doesn't keep the item blocked
- **WHEN** the expiry email can't be sent
- **THEN** the failure is logged and the reservation still ends

#### Scenario: The owner learns nothing
- **WHEN** a reminder or expiry email is sent
- **THEN** it goes only to the reserver, and nothing in the owner's responses or emails reveals the reservation

### Requirement: Keep a reservation
The system SHALL let the reserver of an active, not yet expired reservation keep it, which gives it a new end date (6 months from that moment, rounded up to the next monthly run) and a new reminder a month before.

#### Scenario: Reserver keeps the reservation
- **WHEN** the reserver chooses "Keep reservation" on an item they reserved
- **THEN** the reservation gets a new end date, and a new reminder will be sent one month before it

#### Scenario: Only the reserver can keep it
- **WHEN** someone other than the reserver tries to keep a reservation
- **THEN** the system returns a 403 Forbidden error

#### Scenario: Expired or bought claims can't be kept
- **WHEN** the reserver tries to keep a reservation that has already expired, or a claim marked "Bought"
- **THEN** the system refuses, and an expired item can only be reserved again

### Requirement: Reserver sees when their reservation ends
The system SHALL show the reserver when their reservation ends. Other members SHALL see only that the item is reserved, and the owner SHALL see nothing.

#### Scenario: Reserver sees the end date
- **WHEN** a member views a wishlist with an item they reserved
- **THEN** the item shows the date the reservation ends, and offers "Keep reservation" once the reminder is due (one month or less left)

#### Scenario: Others don't see the end date
- **WHEN** another member views the same item
- **THEN** it shows "Reserved" with no end date

