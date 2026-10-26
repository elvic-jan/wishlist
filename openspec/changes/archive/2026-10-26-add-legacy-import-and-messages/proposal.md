## Why

Before GiftCircles, the family used an older wishlist site (verlanglijstje.shorturl.com). Many people still have wishes there, and retyping them is tedious. They should be able to bring their old wishes over once, in a few taps.

To tell people about this, and later about new features, the app needs a way to show messages that stay until they're dealt with, instead of today's toasts that disappear on their own.

## What Changes

- **Loading the old data (once, by the maintainer).** A job-mode command reads the exported Excel files and stores the old usernames and their live items in two new tables:
  - **Items left out:** struck-out (status 3) and deleted items.
  - **Fields left out:** passwords, email addresses, priority, remarks and reservations.
  - **Not in git:** the export files stay on the maintainer's computer.
- **Import page** (`/import`), reachable from a message and from the account menu:
  1. The user types their old username (capital letters don't matter).
  2. They see their old items, with tick boxes. Items changed since 1 January 2024 are ticked; older ones are listed but not ticked.
  3. The ticked items become normal wishlist items: the old name becomes the title, the old description becomes the description, and links in the description become item links. Old reservations don't carry over.
- **Once only:** each old username can be claimed once, and each account can import once. Afterwards the page says what was imported and from which old username. The maintainer can release a wrong claim by hand.
- **Messages:** a general system for messages that stay until handled.
  - **Defined in code:** each message has an id, title, text, an optional button (label and link), an optional start and end date, and a rule for who sees it.
  - **Shown as toasts** in the app, one at a time, with three choices:
    - the **button**, which takes you somewhere and doesn't show the message again;
    - **✕ Dismiss**, which hides it for good on every device;
    - **Remind me next time**, which hides it until the app is next opened.
- **First messages:**
  - "Bring your wishes from the old site", for accounts that haven't imported yet.
  - A "What's new" message about invite sharing and reservation reminders, for everyone.

## Capabilities

### New Capabilities
- `legacy-import`: loading the old site's export, and the once-only import of a person's old items into their wishlist.
- `messages`: code-defined messages with audience rules, shown as lasting toasts with an action, dismiss and remind-me-next-time.

### Modified Capabilities
<!-- none -->

## Impact

- **Database**: new tables `LegacyUsers`, `LegacyItems` and `UserMessageStates`; one migration.
- **Application**:
  - load, preview and import use cases for the old site;
  - a message registry, an "eligible messages" query and a dismiss/act use case.
- **API**: `GET /api/legacy-import`, `POST /api/legacy-import/preview`, `POST /api/legacy-import`, `GET /api/messages`, `POST /api/messages/{id}/dismiss`. A new job, `--job legacy-load`, run once by hand.
- **Web**: the `/import` page, an account-menu item, and a message toaster in the app shell.
- **Privacy**: the old export's emails and password hashes never reach production. Old usernames and item texts do. On the old site every family member could already see everyone's list.
