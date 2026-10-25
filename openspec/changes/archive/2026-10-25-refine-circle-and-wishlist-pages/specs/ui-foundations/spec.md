## ADDED Requirements

### Requirement: Circle page layout
The circle page SHALL show the circle's name as its heading, list the members with the signed-in user first, let each member row open that member's wishlist (the user's own row opens their wishlist for editing), and list the other circles the user belongs to.

#### Scenario: Heading shows the circle name
- **WHEN** a member opens a circle
- **THEN** the page heading is the circle's name

#### Scenario: Yourself first, editable
- **WHEN** a member views the members of a circle
- **THEN** their own entry is listed first and opens their own wishlist for editing

#### Scenario: Member rows are single buttons
- **WHEN** a member views the members of a circle
- **THEN** each other member is shown as one button displaying their name that opens their wishlist

#### Scenario: Other circles
- **WHEN** a member who belongs to several circles opens one of them
- **THEN** the page lists the other circles they belong to, each opening that circle, and no longer shows a separate "Other wishlists" list

### Requirement: Back returns to the previous screen
Back buttons on wishlist and circle pages SHALL return to the screen the user came from within the app, and SHALL go to "My circles" when there is no previous screen in the app.

#### Scenario: Back after opening a wishlist from a circle
- **WHEN** a user opens a member's wishlist from a circle page and presses Back
- **THEN** the circle page is shown again

#### Scenario: Back on a page opened directly
- **WHEN** a user opens a wishlist page directly from a link and presses Back
- **THEN** "My circles" is shown

### Requirement: Lists don't show an empty state before loading
Pages that load a list SHALL show a loading placeholder until the data has arrived, and SHALL show an empty state only when the loaded list is actually empty.

#### Scenario: No empty-state flash
- **WHEN** a wishlist page is opened or returned to and its items are still loading
- **THEN** a loading placeholder is shown, not "No items yet" or "Nothing here yet"
