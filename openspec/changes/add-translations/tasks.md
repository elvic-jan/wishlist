## 1. Server foundation

- [x] 1.1 Add `Language` (supported codes `en`, `nl`; parse from a tag such as `nl-BE`; fallback `en`) and `Texts.T(language, template, args)` with the Dutch dictionary in `Wishlist.Application/Localization`
- [x] 1.2 Add nullable `User.Language`, EF configuration (max length 8) and a migration
- [x] 1.3 Add `RequestLanguage` in the API (from `Accept-Language`), registered per request
- [x] 1.4 Include `language` in the signed-in user payload; add `PUT /api/auth/me/language` (validates the supported list)
- [x] 1.5 Store the request language on new accounts (password and passkey registration)

## 2. Server texts

- [x] 2.1 Give `DomainException` and subclasses a template + arguments overload; move the interpolated throw sites to it
- [x] 2.2 Localise error responses in `ErrorHandlingMiddleware` (including the generic 500 text)
- [x] 2.3 Add `EmailLanguage.For(recipient, sender)` and pass a language into every email builder (account, password reset/changed, passkey added, buddy, reservation reminder/ended, circle invitation); format dates with the language's culture
- [x] 2.4 Localise lasting-toast message definitions with the request language
- [x] 2.5 Localise the invite link preview in `SpaPage` using the inviter's language
- [x] 2.6 Write the Dutch dictionary entries for all of the above

## 3. Web foundation

- [x] 3.1 Add `src/lib/i18n` (`en.ts`, `nl.ts` typed against `en`, `LanguageProvider`, `useT`, `t`, plural and rich-text helpers)
- [x] 3.2 Resolve the language (account → stored → browser → `en`), set `<html lang>`, persist to `localStorage`
- [x] 3.3 Send `Accept-Language` from the API client and raw API `fetch` calls
- [x] 3.4 Sync with the account on sign-in / session restore and on switching (`PUT /api/auth/me/language`)
- [x] 3.5 Add the language switch to the account menu and to the signed-out pages

## 4. Web texts

- [x] 4.1 Convert the shell and shared components (AppShell, banners, toasters, install prompt, dialogs, invite menu, wishlist editor)
- [x] 4.2 Convert the signed-out pages (login, register, forgot password, recover, confirm email, join)
- [x] 4.3 Convert the signed-in pages (wishlists, my wishlist, member wishlist, circles home, circle detail, security, import, admin, licences, what's new)
- [x] 4.4 Convert helper texts (`inviteMessage`, `inviteErrors`, `webauthn`, `appUpdate`, API error fallbacks) and all `Intl` formatters
- [x] 4.5 Write the Dutch catalog using the confirmed glossary
- [x] 4.6 Sweep for leftover literals (JSX text, `title`, `placeholder`, `aria-label`, toast texts)
- [x] 4.7 Mark wish titles and descriptions (and other user-entered text) `translate="no"` wherever they are shown

## 5. Release notes

- [x] 5.1 Parse `nl:` lines in `scripts/release-version.mjs`; write `notes: [{ en, nl }]`; read file content from `HEAD` when the file still exists; keep `--notes` English
- [x] 5.2 Show notes in the active language on the What's new page (accept both old string and new object shape)
- [x] 5.3 Require an `nl:` line per note in `ReleaseNoteTests` and the pull request check; update `release-notes/README.md`
- [x] 5.4 Translate the existing note files

## 6. Tests and verification

- [x] 6.1 Server tests: language parsing, saving the account language, error response in Dutch, email language fallback (recipient → sender → English), registration stores the language
- [x] 6.2 Test that every user-facing server template has a Dutch entry and no Dutch entry is orphaned
- [x] 6.3 `npm run build`, `npm run lint`, `dotnet test` green
- [ ] 6.4 Walk the app in Dutch and English in the browser (desktop and 375px), including switching and a reload (done for the signed-out pages and What's new on a local build; the signed-in pages need a database, so they are checked on staging in 7.3)
- [x] 6.5 Update the README sections that describe texts, emails or release notes

## 7. Release

- [x] 7.1 Add `release-notes/translations.md` (type `feature`) with the English and Dutch wording approved by the user
- [x] 7.2 Open the pull request with "Closes #105" and the release note in the description
- [ ] 7.3 Check on staging in both languages before merge
