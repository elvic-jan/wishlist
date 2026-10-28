## ADDED Requirements

### Requirement: Wishlist item fields have length limits
When an item is added or edited (by the owner or a buddy), the system SHALL refuse with 400 Bad Request, before storing anything, a title longer than 300 characters, a description longer than 5000 characters, or a URL longer than 2048 characters. Items stored earlier with longer values SHALL still be shown. The SPA's inputs SHALL not accept more than these limits.

#### Scenario: Overlong title
- **WHEN** a user adds an item with a 301-character title
- **THEN** the system returns 400 with a message naming the limit, not 500, and creates nothing

#### Scenario: Overlong description
- **WHEN** a user edits an item and sets a description longer than 5000 characters
- **THEN** the system returns 400 and keeps the item as it was

#### Scenario: Overlong URL
- **WHEN** a user adds an item with a URL longer than 2048 characters
- **THEN** the system returns 400 and creates nothing
