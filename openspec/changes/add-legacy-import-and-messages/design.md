## Context

The old site's export is three Excel files on the maintainer's PC:
- `vl_users.xlsx`: 79 rows. `USER_ID`, `NAME` (username, upper case), `PASSWORD` (unsalted MD5), `DISPLAYNAME`, `EMAILADDRESS`, `ISPUBLIC`.
- `vl_items.xlsx`: 574 rows. `ITEM_ID`, `OWNER` (→ `USER_ID`), `NAME`, `DESCRIPTION` (up to 864 characters; 132 contain links), `STATE` (1 available, 2 reserved, 3 struck out), `STATE_TS` (Excel serial date), `DELETED`, `PRIO`, `REMARK`, and others that are unused.
- `vl_status.xlsx`: the three statuses.

474 items are live (not struck out, not deleted). 301 were changed since 2024, by 25 people.

GiftCircles accounts have an email address and a display name, but no username. The API image already has a job mode (`--job reservations`). Toasts today come from shadcn's toaster (`TOAST_LIMIT = 1`) and are transient. The app shell also shows account banners (confirm email, set up a passkey).

## Goals / Non-Goals

**Goals:**
- A one-time, self-service import of a person's live old items, identified by their old username.
- A small, reusable system for messages that stay until handled.

**Non-Goals:**
- Importing reservations, priorities, remarks, old accounts or passwords.
- Matching people automatically (by email or name).
- An admin screen for messages or for releasing claims (that's the admin section, a separate to-do).
- Moving the existing account banners into the messages system.

## Decisions

### 1. Loading: a job-mode command, run once against production
`--job legacy-load --users <path> --items <path>` reads the two `.xlsx` files and fills the tables. A small reader built on `System.IO.Compression` and `XDocument` (shared strings plus the first sheet) avoids a spreadsheet library.

The maintainer runs it from their PC against production, with a temporary Postgres firewall rule. The same steps the deploy workflow uses are documented in `infra/README.md`.

Only what's needed is stored:
- `LegacyUser(LegacyId int PK, UserName text unique (upper case), ClaimedByUserId Guid? unique, ClaimedAt?)`.
- `LegacyItem(LegacyId int PK, LegacyOwnerId → LegacyUser, Title, Description, ChangedAt)`, for live items only (`STATE` ≠ 3, `DELETED` ≠ 1).

`ChangedAt` comes from `STATE_TS`. Emails, display names, passwords, priority, remark and state are never read into the database.

**Re-running is safe:** rows are upserted by `LegacyId`, and existing claims are kept. If the tables already hold data, the job only adds what's missing.

*Why not a migration with data?* The data would end up in git. *Why not an upload screen?* There's no admin role yet, and it's a one-off.

### 2. Claiming and importing
- **`GET /api/legacy-import`** returns the account's state:
  - `{ imported: false }`, or
  - `{ imported: true, userName, importedAt, count }`, or
  - `{ available: false }` when no old data is loaded at all.
- **`POST /api/legacy-import/preview { userName }`** returns that username's live items: `{ legacyId, title, description, urls, changedAt, suggested }`. `suggested` means changed on or after 2024-01-01. The username is matched case-insensitively, after trimming.
  - Unknown username → 404 "We couldn't find that username."
  - Already claimed by someone else → 409 "This old account has already been imported."
  - The preview claims nothing.
- **`POST /api/legacy-import { userName, itemIds }`** runs in one transaction:
  1. Check the account hasn't imported yet, and the username exists and is unclaimed. A unique index on `ClaimedByUserId` and a conditional update guard against races.
  2. Mark the username as claimed by the account.
  3. Create one wishlist item per ticked id that belongs to that username. Ids that don't belong to it are ignored.

  Ticking nothing is allowed: it claims the username with zero items, after a confirmation in the UI.
- **Field mapping:**
  - Title: old `NAME`, trimmed and cut to the title limit.
  - Description: old `DESCRIPTION`, with the few HTML tags stripped to text, and links removed from the text.
  - Links: every `http(s)://…` or `www.…` in the description, up to the item's link limit, de-duplicated.
  - Item `CreatedAt` is the import time.
- **Guessing usernames:** the preview is limited per account (10 unknown usernames per hour) to stop someone listing the old usernames. Old lists were family-visible anyway, so this only keeps it from being trivial.
- **Releasing a wrong claim:** by hand, with SQL (`UPDATE "LegacyUsers" SET "ClaimedByUserId" = NULL …`), documented in `infra/README.md`. The items already imported stay with the account that imported them, which can delete them.

### 3. Messages: a registry in code, state in the database
```
IMessageDefinition
  Id                "legacy-import", "whats-new-2026-11"
  Title, Text
  Action?           { Label, Url }  (in-app path)
  ShowFrom?/Until?  dates
  AppliesToAsync(user, ct)          audience rule
```
- **Definitions** are classes in `Application/Messages`, registered in DI. Adding a message means adding a class.
- **`UserMessageState(UserId, MessageId, DismissedAt)`**, keyed on `(UserId, MessageId)`. Using the action and dismissing both store `DismissedAt`, so the message never comes back.
- **`GET /api/messages`** returns the current user's messages, oldest first: in their date window, applying to them, and not dismissed.
- **`POST /api/messages/{id}/dismiss`** stores the state. It's idempotent, and unknown ids get a 404.
- **"Remind me next time"** is client-only: the id goes into `sessionStorage` for this app session. Opening the app again (new tab, or relaunching the installed app) shows it again. If storage isn't available, the message simply shows again on the next page load.

First messages:
- **`legacy-import`:** "Bring your wishes from the old site". Shown when old data is loaded and the account hasn't imported. Action "Import my wishes" → `/import`.
- **`whats-new-2026-11`:** "What's new". Invites can now be shared straight to WhatsApp or email, and you'll get a reminder before a reservation runs out. Shown to everyone until 2026-12-31. No action.

### 4. Message toasts in the UI
A `MessageToaster` in the app shell, separate from the transient toaster:
- It fetches `/api/messages` once per app load and shows **one message at a time**, bottom-centre on phones and bottom-right on wider screens.
- Each message has its action button, "Remind me next time" and ✕.
- Choosing any of them shows the next message.
- It's hidden on the import page itself.
- Styled with the theme tokens, like the existing cards.

### 5. Account menu
A new menu item, "Import from old site", goes to `/import`. After importing, the page shows the result, and the menu item stays (it's harmless).

## Risks / Trade-offs

- **Someone claims another person's old username.** The real owner sees "already imported" and asks the maintainer, who releases it by SQL. Acceptable for a family app, and recorded with `ClaimedAt` and the claiming account.
- **Old usernames in production.** Low sensitivity: no emails or passwords are stored.
- **Running the load job against production** needs a temporary firewall rule. It's documented step by step and removed afterwards.
- **Old descriptions with odd HTML.** Only 4 of them have HTML; they're stripped to text.

## Migration Plan

1. Deploy: the migration creates the three tables. With no old data loaded, the import page says nothing is available, and the `legacy-import` message isn't shown.
2. The maintainer runs `--job legacy-load` once against production, following `infra/README.md`.
3. From then on, people see the message and can import.

Rollback: the tables are additive. Old app versions ignore them.
