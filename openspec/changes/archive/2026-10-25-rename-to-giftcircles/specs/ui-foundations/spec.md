## ADDED Requirements

### Requirement: Product name and circle terminology
The application SHALL present itself to users as "GiftCircles" and SHALL call a group of people who share wishlists a "circle" in every user-facing text, including page content, navigation, page titles, user-visible API error messages and emails. Internal names (code, API routes, database tables) MAY keep the word "group".

#### Scenario: App name in the shell and browser tab
- **WHEN** a user opens any page of the application
- **THEN** the header and the browser tab title show "GiftCircles" and not "Wishlist"

#### Scenario: Circles in navigation and pages
- **WHEN** a signed-in user views the navigation, the home page or a circle's page
- **THEN** the text refers to "circles" (for example "My circles", "Create circle", "Leave circle") and not to "groups"

#### Scenario: Circle page address
- **WHEN** a user opens a circle
- **THEN** the address is `/circles/{id}`

#### Scenario: Old circle address still works
- **WHEN** a user opens a bookmarked `/groups/{id}` address
- **THEN** the application redirects to `/circles/{id}`

#### Scenario: Error messages use the new word
- **WHEN** the API rejects a request because the user is not in the circle
- **THEN** the error message the user sees refers to a "circle"

#### Scenario: Emails use the product name
- **WHEN** the system sends a password-reset or password-changed email
- **THEN** the sender name, subject and body refer to "GiftCircles"
