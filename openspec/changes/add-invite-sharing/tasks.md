## 1. Invitations

- [x] 1.1 `GroupInvitation.Lifetime` = 3 days; the repository uses it (existing rows keep their `ExpiresAt`)
- [x] 1.2 `CheckInvitationUseCase` (extended): resolves a code and returns expiry, inviter display name ("Someone" if gone) and circle name
- [x] 1.3 `GET /api/invitations/{code}` returns `inviterName` and `circleName`

## 2. Link preview

- [x] 2.1 Preview image `public/og/giftcircles.png` (1200×630: white gift icon and "GiftCircles" on terracotta)
- [x] 2.2 Generic `og:` / `twitter:` tags in `index.html` with an absolute image URL
- [x] 2.3 `GET /join/{code}` endpoint before the SPA fallback: cached `index.html` with personal tags for a usable code (through `InviteCodeGuard`), generic tags otherwise; HTML-encoded; `Cache-Control: no-store`

## 3. Web

- [x] 3.1 `buildInviteMessage` (message, email subject, WhatsApp and mailto URLs), with the "works for N days" text from `expiresAt`
- [x] 3.2 Circle page: "Invite people" menu creating a link, with Share… (only if `navigator.share` exists; ignore cancel), WhatsApp, Email and Copy link
- [x] 3.3 Join page: "<inviter> invited you to <circle>"

## 4. Tests and verification

- [x] 4.1 Tests: new links expire after 3 days; old 7-day links still work; check returns inviter and circle ("Someone" fallback); `/join/{code}` personal tags for a valid code, generic for unknown, expired and rate-limited codes; names are HTML-encoded
- [x] 4.2 Build, tests, SPA lint and build
- [x] 4.3 Browser check (phone width): menu options, message text, WhatsApp and mailto URLs, copy; join page text; preview tags in the HTML of `/join/{code}`
- [ ] 4.4 After deploy (user): share a link in WhatsApp and see the preview card
