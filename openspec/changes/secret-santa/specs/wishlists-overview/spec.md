## MODIFIED Requirements

### Requirement: Wishlists page
The SPA SHALL offer a Wishlists page at `/wishlists` with a link to the user's own wishlist at the top, followed by the people the user drew in their active Secret Santa drawings, and then the other people the user shares a circle with. Each person the user drew SHALL be shown once, at that position instead of in the alphabetical list, marked with the title of the drawing (or drawings) in which the user drew them. Each person SHALL show their name and their shared circles as badges, and SHALL open that person's wishlist when clicked. A drawn person who hasn't joined the drawing yet and shares no other circle with the user SHALL be shown with their name and the drawing's title, marked as not joined yet, and SHALL NOT open a wishlist. While the list loads the page SHALL show placeholders, never the empty message.

#### Scenario: Open my own wishlist
- **WHEN** a user on the Wishlists page clicks "My wishlist"
- **THEN** the SPA opens `/wishlist/me`

#### Scenario: Open someone's wishlist
- **WHEN** a user on the Wishlists page clicks a person
- **THEN** the SPA opens `/wishlist/{userId}` for that person

#### Scenario: Drawn person on top
- **WHEN** a user drew Piet in "Christmas 2026" and also shares the circle Family with Anna and Piet
- **THEN** the page shows "My wishlist", then Piet marked "Christmas 2026", then Anna, and Piet does not appear a second time

#### Scenario: Drawn person not joined yet
- **WHEN** the user drew Piet, who hasn't joined the drawing and shares no other circle with the user
- **THEN** Piet is shown on top with "Christmas 2026", marked as not joined yet, and can't be opened

#### Scenario: Drawing no longer visible
- **WHEN** the drawing is cancelled or more than a month past its end date
- **THEN** the drawn person is no longer shown on top

#### Scenario: Loading
- **WHEN** the Wishlists page is opened and the list has not arrived yet
- **THEN** the page shows placeholder rows and no "nobody yet" message

#### Scenario: Nobody to show
- **WHEN** the user shares no circle with anyone and has drawn nobody
- **THEN** the page still shows "My wishlist", says there is nobody yet, and points to My circles to create or join a circle
