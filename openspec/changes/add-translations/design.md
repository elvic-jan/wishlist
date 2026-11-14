## Context

All texts are string literals: about 7,500 lines of React (pages, components), 8 emails built in the Application layer, about 70 user-facing exception messages returned as `{ error }` by `ErrorHandlingMiddleware`, 4 lasting-toast message definitions, the invite link preview in `SpaPage`, and release notes generated into `/releases.json` at deploy time. There is no notion of a user's language anywhere. The Content-Security-Policy allows only same-origin scripts, and the project keeps dependencies few.

Product decisions (user, 2026-11-14): English + Dutch; browser language with a switch saved to the account; emails in the recipient's language, else the sender's, else English; release notes in every language, existing ones translated once.

## Goals / Non-Goals

**Goals:**
- Every app-produced text available in English and Dutch, with one place per side (web, server) holding the texts.
- Missing translations are caught by the compiler or a test, not by users.
- A third language is a matter of adding catalogs.

**Non-Goals:**
- Translating user content (wishes, names, circle names).
- Translating operator-only output: job/CLI messages (`--job …`), logs, the GitHub Release text, the open-source licences list.
- Per-region variants (nl-BE vs nl-NL), right-to-left support.
- Translated URLs.

## Decisions

### 1. Web: own small typed catalog module, no i18n library
`src/lib/i18n/` holds `en.ts` (the source of truth, a flat object of `key: 'text with {placeholders}'`), `nl.ts` typed as `Record<keyof typeof en, string>` so a missing or extra Dutch key is a compile error, and `index.ts` with `LanguageProvider`, `useT()` and a non-hook `t()` for code outside React (toasts from helpers, `inviteMessage.ts`, `webauthn.ts`). Plurals use `Intl.PluralRules` with `key_one` / `key_other` pairs. Rich text (a bold name inside a sentence) uses a tiny `<Trans>` helper that splits on `<b>…</b>`-style markers, so sentences stay whole for translators.

*Why not react-i18next / FormatJS:* two languages and ~600 strings don't need them; they add bundle weight and a runtime we would mostly configure away. Typed keys give the safety we need. Both catalogs are bundled (a few kB gzipped), so switching is instant and works offline.

### 2. Language resolution on the web
Order: account language (when signed in and set) → `localStorage` `kadoozle-language` → first supported entry of `navigator.languages` → `en`. Changing the switch writes `localStorage`, sets `<html lang>`, and when signed in calls `PUT /api/account/language`. On sign-in or session restore: an account with a language overrides the device; an account without one is given the device's current language (so emails match what the person sees). `Intl.DateTimeFormat` and friends take the active language instead of `undefined`.

### 3. The server learns the request language from `Accept-Language`
The axios client (and the few raw `fetch` calls to the API) send `Accept-Language: <active language>`. A small `RequestLanguage` service in the API resolves, in order: the signed-in user's saved language is **not** consulted per request (no extra query); the header is the truth for responses, since the client already folds the account language into it. Unsupported or missing → `en`.

### 4. Server texts: one catalog in Application, English text as key
`Wishlist.Application/Localization`: `Language` (supported codes, parsing, fallback), `Texts` with `T(language, "English template with {0}", args)`, and a Dutch dictionary `english template → dutch template`. English needs no dictionary.

- **Errors**: `DomainException` (and subclasses) get an overload taking a template and arguments; `Message` stays the formatted English (logs and existing tests unchanged) and the template + args are kept for the middleware, which answers in the request language. The five interpolated throw sites move to the overload; literal ones need no change.
- **Emails**: each email builder takes a `Language` and writes its subject and bodies through `T`. Dates in emails are formatted with the language's culture.
- **Lasting-toast messages**: `IMessageDefinition` texts go through `T` with the request language.
- **Link preview** (`SpaPage` for `/join/{code}`): in the inviter's language (the sender rule), else English; crawlers send no useful language.

*Why English-as-key instead of named keys:* ~70 throw sites stay untouched and readable, and tests asserting English messages keep working. The risk (editing an English text silently orphans its Dutch one) is covered by a test that scans the Application/API/Domain sources for user-facing exception literals and `T(` templates and fails when one has no Dutch entry, or a Dutch entry matches nothing.

*Why server-side rather than error codes translated on the web:* the server must translate anyway (emails, previews), and codes would mean touching every throw site and every catch on the web.

### 5. The account's language
`User.Language` (`varchar(8)`, nullable; null = never chosen). EF migration, no backfill. The user payload returned by sign-in/refresh/me includes `language`. `PUT /api/account/language { language }` validates against the supported list. Registration (password and passkey) stores the request language, so a new account's first emails are right.

### 6. Email language rule in one place
`EmailLanguage.For(recipient, sender?)` = `recipient.Language ?? sender?.Language ?? en`. Uses:
- confirm email, passkey added, password changed: recipient, else the request language (the recipient is the one acting).
- password reset / recovery link: recipient's saved language, else the request language.
- buddy added, circle invitation: recipient, else the owner / inviter.
- reservation reminder and ended (monthly job, no sender): recipient, else English.

### 7. Release notes in every language
File format: each note line is followed by its translation line(s) prefixed with the language code:

```
---
type: feature
---
You can now sort your wishlist
nl: Je kan je verlanglijst nu sorteren
```

`release-version.mjs` parses this into `notes: [{ en, nl }]` in `releases.json`; the What's new page picks the active language and falls back to `en`. The 200-character limit applies per line. `--notes` (GitHub Release) stays English. The PR check and `ReleaseNoteTests` fail when a note line has no `nl:` line.

Past releases read their files from git **at the tag**, where no Dutch lines exist. To translate old notes once, the script keeps deciding *which files belong to a release* from the tags but reads each file's *content* from `HEAD` when the file still exists there (falling back to the tag). The README's "don't edit old ones" becomes "only edit old ones to fix wording or translations".

### 8. Dutch wording
Informal ("je"), Belgian-neutral Dutch. A short glossary at the top of the Dutch catalog keeps terms consistent (wishlist, wish, circle, buddy, reserve, passkey, invite). The glossary is confirmed with the user before the catalog is written.

## Risks / Trade-offs

- [A literal is missed and stays English] → after the conversion, grep the web sources for JSX text and string props (`title`, `placeholder`, `aria-label`, `description`) outside the catalogs; walk every page in Dutch in the browser.
- [Dutch texts are longer and break layouts] → check narrow (375px) viewport on the main pages in Dutch.
- [English-as-key drift on the server] → the source-scanning test in decision 4.
- [Old clients cached by the service worker read the new `releases.json` shape] → the page accepts both a string and an object per note; the script change ships together with the page.
- [A release note PR forgets Dutch] → check fails with a message showing the expected format.
- [Emails to invitees in the sender's language may be the wrong guess] → accepted (user decision); the invitee's own choice applies from their first visit.

## Migration Plan

One additive migration (`Users.Language` nullable). Deploy as usual; no data backfill — existing accounts get their language saved at their next visit. Rollback: redeploy the previous release; the extra column is ignored.

## Open Questions

- Dutch glossary (decision 8) — asked before writing the catalog.
