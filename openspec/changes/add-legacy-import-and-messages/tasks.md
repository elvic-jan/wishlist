## 1. Data and loading

- [x] 1.1 Entities `LegacyUser`, `LegacyItem`, `UserMessageState`; EF configuration (unique `UserName`, unique nullable `ClaimedByUserId`, PK `(UserId, MessageId)`); migration
- [x] 1.2 Minimal `.xlsx` reader (zip + shared strings + first sheet) in Infrastructure
- [x] 1.3 `LoadLegacyDataUseCase` + `--job legacy-load --users <path> --items <path>`: live items only, no emails/names/passwords/prio/remark/state; upsert by legacy id; keeps claims; logs counts
- [x] 1.4 `infra/README.md`: how to run the load once against production (temporary firewall rule, connection string, cleanup) and how to release a wrong claim

## 2. Import

- [x] 2.1 Mapping: title (trim, cut to limit), description (strip HTML, remove links from text), links extracted and de-duplicated up to the item's limit
- [x] 2.2 `GetLegacyImportStatusUseCase`, `PreviewLegacyImportUseCase` (case-insensitive match, 404/409, `suggested` since 2024-01-01, unknown-username limit 10/hour per account), `ImportLegacyItemsUseCase` (one transaction; once per account and per username; ignores foreign ids)
- [x] 2.3 Endpoints `GET /api/legacy-import`, `POST /api/legacy-import/preview`, `POST /api/legacy-import`

## 3. Messages

- [x] 3.1 `IMessageDefinition`, registry, `GetMyMessagesUseCase`, `DismissMessageUseCase`; endpoints `GET /api/messages`, `POST /api/messages/{id}/dismiss`
- [x] 3.2 Messages `legacy-import` (old data loaded and not imported; action → `/import`) and `whats-new-2026-11` (everyone until 2026-12-31)

## 4. Web

- [x] 4.1 `/import` page: status, username entry, item list with tick boxes (suggested ticked), confirm (also with nothing ticked), result; "not available" state
- [x] 4.2 Account menu item "Import from old site"
- [x] 4.3 `MessageToaster` in the app shell: one at a time, action / "Remind me next time" (sessionStorage) / close; hidden on `/import`

## 5. Tests and verification

- [x] 5.1 Tests: xlsx reader and load (live only, sensitive columns absent, re-run keeps claims); mapping (HTML, links); preview (case-insensitive, 404, 409, limit); import (ticked only, foreign ids ignored, once per account and per username, race); messages (audience, window, dismiss idempotent, 404, import message disappears after import)
- [x] 5.2 Build, tests (with Postgres), SPA lint and build
- [x] 5.3 Local run: load the real exports into the local database with the job (79 usernames, 475 live items, only username/item columns stored); import path checked against Postgres in a test; import page and message toasts at phone width with a faked API (action, remind, close, look-up, ticks, import)
- [ ] 5.4 After deploy (user): run the load job against production, then check the message and an import
