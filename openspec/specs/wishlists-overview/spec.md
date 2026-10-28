# wishlists-overview Specification

## Purpose
Lists everyone the user shares a circle with, each once with the circles they share, on a filterable Wishlists page reached from the top menu; each name opens that person's wishlist, whose title shows their name.
## Requirements
### Requirement: List the people I share a circle with
The system SHALL let an authenticated user list every other user who shares at least one circle with them. Each person SHALL appear once, with their display name and the circles they share with the user. The user SHALL NOT appear in their own list. People SHALL be sorted by display name, ignoring case, and each person's circles by name.

#### Scenario: Person in several shared circles appears once
- **WHEN** a user shares the circles Family and Friends with Anna and requests their people list
- **THEN** the system returns Anna once, with the circles Family and Friends

#### Scenario: Only the circles the user shares are listed
- **WHEN** Anna is in Family and Work, and the user is in Family only
- **THEN** Anna is listed with Family only

#### Scenario: Strangers and the user are not listed
- **WHEN** a user requests their people list
- **THEN** the system leaves out the user themself and everyone who shares no circle with them

#### Scenario: Alphabetical order
- **WHEN** the user's circle-mates are named "bob", "Anna" and "Carl"
- **THEN** the list is ordered Anna, bob, Carl

#### Scenario: No circles
- **WHEN** a user who is in no circle, or only in circles where they are alone, requests their people list
- **THEN** the system returns an empty list

#### Scenario: Not signed in
- **WHEN** a request for the people list carries no valid session
- **THEN** the system returns 401 Unauthorized

### Requirement: Wishlists page
The SPA SHALL offer a Wishlists page at `/wishlists` with a link to the user's own wishlist at the top, followed by the people the user shares a circle with. Each person SHALL show their name and their shared circles as badges, and SHALL open that person's wishlist when clicked. While the list loads the page SHALL show placeholders, never the empty message.

#### Scenario: Open my own wishlist
- **WHEN** a user on the Wishlists page clicks "My wishlist"
- **THEN** the SPA opens `/wishlist/me`

#### Scenario: Open someone's wishlist
- **WHEN** a user on the Wishlists page clicks a person
- **THEN** the SPA opens `/wishlist/{userId}` for that person

#### Scenario: Loading
- **WHEN** the Wishlists page is opened and the list has not arrived yet
- **THEN** the page shows placeholder rows and no "nobody yet" message

#### Scenario: Nobody to show
- **WHEN** the user shares no circle with anyone
- **THEN** the page still shows "My wishlist", says there is nobody yet, and points to My circles to create or join a circle

### Requirement: Filter the Wishlists page
The Wishlists page SHALL have a text filter that keeps the people whose name, or the name of one of their shared circles, contains the filter text, ignoring case and accents. "My wishlist" SHALL stay visible while filtering.

#### Scenario: Filter by name
- **WHEN** the user types "ann" and Anna and Johann are listed
- **THEN** both Anna and Johann stay and the others are hidden

#### Scenario: Filter by circle
- **WHEN** the user types "family"
- **THEN** everyone who shares the Family circle with the user stays

#### Scenario: Nothing matches
- **WHEN** no person matches the filter
- **THEN** the page says that nobody matches the filter

### Requirement: Circle badges stay bounded
Each person's circle badges SHALL NOT break the page layout. A badge SHALL be cut off with an ellipsis beyond a fixed width and show the full circle name as a tooltip. At most 5 badges SHALL show per person, followed by a "+N more" badge listing the rest in its tooltip. When the filter matches circle names, the matching badges SHALL be shown first, so none is hidden behind "+N more".

#### Scenario: Long circle name
- **WHEN** a shared circle has a very long name
- **THEN** its badge is cut off with "…" and the page has no horizontal scroll, also at phone width

#### Scenario: Many shared circles
- **WHEN** a person shares 8 circles with the user
- **THEN** 5 badges show, followed by "+3 more"

#### Scenario: Filter matches a hidden circle
- **WHEN** a person shares 8 circles with the user and the filter matches the name of the 8th
- **THEN** that circle's badge is among the badges shown for that person

### Requirement: Wishlists in the top menu
The app shell's top menu SHALL show "Wishlists" instead of "My wishlist", opening `/wishlists`. It SHALL be highlighted on the Wishlists page, on the user's own wishlist and on anyone else's wishlist page. Clicking a member on a circle's page SHALL still open that member's wishlist directly.

#### Scenario: Menu item opens the Wishlists page
- **WHEN** a signed-in user clicks "Wishlists" in the top menu
- **THEN** the SPA opens `/wishlists`

#### Scenario: Highlighted on wishlist pages
- **WHEN** the user is on `/wishlists`, `/wishlist/me` or `/wishlist/{userId}`
- **THEN** "Wishlists" is highlighted in the top menu and "Circles" is not

#### Scenario: Circle member list unchanged
- **WHEN** a user clicks a member's name on a circle's page
- **THEN** the SPA opens that member's wishlist, as before

### Requirement: Owner's name on their wishlist page
Someone's wishlist page SHALL show their name in the title ("Anna's wishlist") for every user who shares a circle with them, not only for their buddies. When the name is not known (still loading, or the list could not be loaded) the title SHALL be "Wishlist".

#### Scenario: Circle-mate opens a wishlist
- **WHEN** a user who shares a circle with Anna, and is not her buddy, opens Anna's wishlist
- **THEN** the title reads "Anna's wishlist"

#### Scenario: Name not known
- **WHEN** the people list has not arrived yet or could not be loaded
- **THEN** the title reads "Wishlist"

