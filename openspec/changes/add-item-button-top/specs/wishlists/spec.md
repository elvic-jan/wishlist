## ADDED Requirements

### Requirement: Add button placement
On a wishlist the user can edit (their own, or one they are a buddy for), the system SHALL show the add button at the top of the list whenever the list has items, and also below the last item when the list has 4 or more items. The new-item form SHALL open where the clicked button is.

#### Scenario: Short list
- **WHEN** the user opens an editable wishlist with 1 to 3 items
- **THEN** the add button is shown above the items and not below them

#### Scenario: Long list
- **WHEN** the user opens an editable wishlist with 4 or more items
- **THEN** the add button is shown both above the first item and below the last item

#### Scenario: Empty list
- **WHEN** the user opens an editable wishlist with no items
- **THEN** only the empty-state message with its add button is shown

#### Scenario: Form opens at the clicked button
- **WHEN** the user clicks the add button at the top (or at the bottom)
- **THEN** the new-item form opens in that place, replacing the button

#### Scenario: Adding confirms in place
- **WHEN** the user adds an item
- **THEN** the form closes, the page stays where it is, a short "Added" confirmation is shown, and the item appears at the end of the list
