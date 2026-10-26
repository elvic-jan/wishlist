## ADDED Requirements

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
