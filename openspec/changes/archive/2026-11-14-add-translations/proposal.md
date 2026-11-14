## Why

Kadoozle is English only, while many of the people using it are Dutch speaking (issue #105). Family members who are less at ease in English should be able to use the app, and read its emails, in their own language.

## What Changes

- The app is offered in **English and Dutch**. Every text the app itself produces is translated: pages, dialogs, menus, toasts, validation and error messages, lasting-toast messages, the invite share text, the link preview of invite links, and all emails. Text that people type themselves (wishes, circle names, display names) is never translated.
- **Choosing the language**: a first-time visitor gets the language of their browser (Dutch for a Dutch browser, otherwise English). A language switch is available in the account menu and on the signed-out pages (sign in, register, recover, join). The choice is saved on the device and, when signed in, on the account, so it follows the person to other devices.
- **Emails** are written in the recipient's saved language. A recipient without one (for example someone who was just invited) gets the language of the person whose action sent the email, and English when there is no such person.
- **Dates and numbers** follow the chosen language instead of the browser's locale.
- **Release notes** are written in every language: each note line in a `release-notes/*.md` file is followed by its Dutch line. The What's new page shows the notes in the reader's language. Existing notes are translated once. Pull request checks fail when a note has no Dutch line.
- The page's `lang` attribute follows the chosen language.
- The structure allows adding a third language later by adding catalogs, without touching pages.

## Capabilities

### New Capabilities
- `translations`: which languages exist, how a person's language is chosen, remembered and changed, what is translated (UI, server-provided texts, emails, link previews), and which language an email uses.

### Modified Capabilities
- `releases`: release note files carry each note in every language and checks enforce it; the What's new page shows notes in the reader's language.

## Impact

- **Web** (`src/Wishlist.Web`): a small typed translation module with `en` and `nl` catalogs; every page and component moves its literals to the catalogs; a language switch; the API client sends the chosen language with each request; `Intl` formatting uses the chosen language.
- **API / Application / Domain**: `User` gains a nullable `Language` (migration); the signed-in user payload includes it; a new endpoint saves it; registration accepts it. Error responses, lasting-toast messages, email subjects and bodies, and the invite link preview are localised on the server from one catalog.
- **Release tooling**: `release-notes/README.md`, `scripts/release-version.mjs` (notes per language in `releases.json`; GitHub Release stays English), the pull request check and `ReleaseNoteTests`.
- **Tests**: catalog completeness (every key in every language, server and web), language selection, email language fallback, release note format.
- No new runtime dependencies.
