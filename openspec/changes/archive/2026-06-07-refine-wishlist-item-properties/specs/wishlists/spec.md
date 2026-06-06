## MODIFIED Requirements

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
The system SHALL allow an authenticated user to view their own wishlist items without any claim status information.

#### Scenario: Own wishlist returned without claim status

- **WHEN** an authenticated user requests their own wishlist
- **THEN** the system returns all their wishlist items with title, description, and the ordered URL list — but with no reservation or purchase status attached

### Requirement: Edit wishlist item
The system SHALL allow an authenticated user to update the title, description, or URL list of an item they own. Editing the URL list replaces it wholesale with the submitted list (preserving the new order).

#### Scenario: Successful edit

- **WHEN** an authenticated user submits updated fields for a wishlist item they own, including optionally a new URL list
- **THEN** the system updates the item, replaces any prior URLs with the newly submitted list in the submitted order, and returns the updated item

#### Scenario: Editing another user's item rejected

- **WHEN** an authenticated user attempts to edit a wishlist item they do not own
- **THEN** the system returns a 403 Forbidden error

#### Scenario: Edit with more than 10 URLs rejected

- **WHEN** an authenticated user submits an edit for a wishlist item they own where the URL list contains 11 or more URLs
- **THEN** the system returns a 400 Bad Request and does not modify the item

#### Scenario: Edit with malformed URL rejected

- **WHEN** an authenticated user submits an edit where any URL in the list is not an absolute `http://` or `https://` URL
- **THEN** the system returns a 400 Bad Request and does not modify the item

