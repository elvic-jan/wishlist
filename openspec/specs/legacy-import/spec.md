# legacy-import Specification

## Purpose
Lets people bring their wishes over, once, from the family's old wishlist site (verlanglijstje.shorturl.com). The old site's export is loaded once by the maintainer, keeping only usernames and live items.

## Requirements
### Requirement: Load the old site's export
The system SHALL provide a job-mode command that reads the old site's exported users and items spreadsheets and stores, for each old user, their username, and for each live item (not struck out, not deleted), its owner, title, description and last-changed date. The command SHALL NOT store email addresses, display names, passwords, priorities, remarks or reservation state. Running it again SHALL keep existing claims.

#### Scenario: Live items only
- **WHEN** the maintainer runs the load command with the users and items exports
- **THEN** every old username is stored, and only items whose state isn't "struck out" and that aren't deleted are stored

#### Scenario: Sensitive columns are left out
- **WHEN** the load command has run
- **THEN** no email address, display name or password hash from the export exists in the database

#### Scenario: Running it again
- **WHEN** the load command runs again after some usernames have been claimed
- **THEN** the claims are unchanged, and no duplicate users or items are created

### Requirement: Preview old items by username
The system SHALL let a signed-in user who hasn't imported yet enter an old username (matched without regard to capital letters) and see that username's live old items, each marked as suggested when changed on or after 1 January 2024. Previewing SHALL NOT claim the username.

#### Scenario: Known username
- **WHEN** a user enters an old username that exists and isn't claimed
- **THEN** the system returns its live items with title, description, links and whether each is suggested

#### Scenario: Unknown username
- **WHEN** a user enters a username that doesn't exist in the old data
- **THEN** the system returns 404 Not Found with "We couldn't find that username."

#### Scenario: Already claimed
- **WHEN** a user enters an old username that another account has already imported
- **THEN** the system returns 409 Conflict with "This old account has already been imported."

#### Scenario: Guessing is limited
- **WHEN** a user has entered more than 10 unknown usernames within an hour
- **THEN** further previews are refused with 429 Too Many Requests until the hour has passed

### Requirement: Import chosen old items once
The system SHALL let a signed-in user import the old items they ticked from one old username into their wishlist, as normal wishlist items with the old name as title, the old description as description, and links found in the description as item links. Each old username SHALL be claimable by one account, and each account SHALL import at most once. Old reservations SHALL NOT be carried over.

#### Scenario: Import ticked items
- **WHEN** a user imports an unclaimed old username with some of its items ticked
- **THEN** exactly the ticked items appear on their wishlist, unreserved, and the old username is recorded as claimed by their account

#### Scenario: Links become item links
- **WHEN** an imported old description contains web addresses
- **THEN** those addresses become the item's links and are removed from the description text

#### Scenario: Items of another username are ignored
- **WHEN** an import request includes item ids that don't belong to the chosen username
- **THEN** those ids are ignored

#### Scenario: Only once per account
- **WHEN** a user who has already imported tries to import again
- **THEN** the system refuses with 409 Conflict and changes nothing

#### Scenario: Only once per old username
- **WHEN** two accounts try to import the same old username
- **THEN** only the first succeeds and the second gets 409 Conflict

#### Scenario: Import status
- **WHEN** a user opens the import page after importing
- **THEN** it shows which old username was imported, when, and how many items

#### Scenario: Nothing loaded
- **WHEN** no old data has been loaded
- **THEN** the import page says importing isn't available, and the import message isn't shown

### Requirement: Import page
The application SHALL offer the import on its own page, reachable from the account menu and from the import message, where the user enters their old username, reviews their old items with tick boxes (suggested ones ticked), and confirms.

#### Scenario: From the account menu
- **WHEN** a signed-in user chooses "Import from old site" in the account menu
- **THEN** the import page opens

#### Scenario: Review before importing
- **WHEN** a user enters a known, unclaimed username
- **THEN** the page lists the old items with suggested ones ticked and others unticked, and nothing is imported until they confirm

