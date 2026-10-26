## Context

`POST /api/groups/{id}/invitations` creates a `GroupInvitation` (random 32-hex code, `ExpiresAt = now + 7 days`) and returns its URL. The circle page copies that URL to the clipboard. Invitations are already usable by several people: `UsedAt` is recorded but never enforced. `GET /api/invitations/{code}` (unauthenticated, behind `InviteCodeGuard`, which rate-limits wrong codes per IP) tells the join page whether a code works.

In production the API serves the SPA from `wwwroot` with `MapFallbackToFile("index.html")`, so `/join/{code}` returns the same static `index.html` as every other page. Link-preview crawlers (WhatsApp, iMessage, Signal, Slack, Facebook) don't run JavaScript: they read `og:` meta tags from that HTML, and there are none.

## Goals / Non-Goals

**Goals:**
- Share an invitation in one or two taps on phones and desktops.
- A personal preview card for invite links, and a generic one for everything else.
- Invitation links last 3 days.

**Non-Goals:**
- GiftCircles sending invitation emails itself (the user's own mail app is used).
- Revoking links, listing active links, or single-use links.
- Contact pickers or importing address books.

## Decisions

### 1. One "Invite people" menu, built on the existing endpoint
The button creates a link (as today, one per click) and then offers the share targets. To avoid creating a link nobody shares, the link is created when the menu opens. Creating a link is cheap and links expire.

| Option | How | Where it works |
|---|---|---|
| Share… | `navigator.share({ title, text, url })` | Android, iPhone/iPad, installed app, Safari/Edge/Chrome on desktop; hidden when `navigator.share` is missing |
| WhatsApp | `https://wa.me/?text=<encoded message + url>` in a new tab | everywhere; opens the app or WhatsApp Web |
| Email | `mailto:?subject=…&body=…` | everywhere with a mail app |
| Copy link | clipboard, as today | everywhere |

The message is built in one place (`buildInviteMessage(inviterName, circleName, url)`) so all four options say the same thing:

> Jan invited you to the Family circle on GiftCircles, where we share wishlists. The link works for 3 days: https://giftcircles.app/join/…

Email subject: "Join my Family circle on GiftCircles". If the user closes the share sheet, nothing happens (no error toast for `AbortError`).

### 2. Three-day lifetime
`GroupInvitation.Lifetime = TimeSpan.FromDays(3)` in the domain, used by the repository. Existing rows keep their stored `ExpiresAt`, so links already sent keep 7 days. The text "The link works for 3 days" is built from the returned `expiresAt`, not hard-coded.

### 3. Who invited you to what
`InvitationStatusDto` gets `InviterName` (the creator's display name) and `CircleName`. The join page uses them: "Jan invited you to **Family**". The values come from the existing `CheckInvitationUseCase`, extended and shared by the check endpoint and the preview (decision 4). If the inviter has since deleted their account, the name falls back to "Someone".

### 4. Personal preview on `/join/{code}`, generic elsewhere
- **Generic tags in `index.html`:** `og:title` "GiftCircles", `og:description` "Share wishlists with your family and friends.", `og:image` `/og/giftcircles.png`, `og:type` website, `twitter:card` `summary_large_image`. These cover every page and the dev server.
- **Personal tags for `/join/{code}`:** a minimal API endpoint, `GET /join/{code}`, mapped before the SPA fallback. It reads `wwwroot/index.html` once (cached in memory), resolves the code through `InviteCodeGuard.TryAsync` + `CheckInvitationUseCase`, and replaces the generic `og:title` / `og:description` / `og:url` with:
  - title: "Jan invited you to Family on GiftCircles"
  - description: "Join the circle to see each other's wishlists and coordinate gifts."

  Values are HTML-encoded. Unknown, expired or rate-limited codes, and any error, return the page with the generic tags. The response is always the normal SPA page with status 200, so the browser experience is unchanged. Sent with `Cache-Control: no-store`.
- **`og:image` must be absolute:** `index.html` holds root-relative paths, and the server serves every SPA page (the fallback replaces `MapFallbackToFile`) with `og:image`, `twitter:image` and `og:url` made absolute from `App:PublicBaseUrl` (`SpaPage.Render`). On the Vite dev server they stay relative, which only matters to crawlers.
- **Preview image:** `public/og/giftcircles.png`, 1200×630: the white gift icon centred on terracotta `#c2410c` with "GiftCircles" beneath it. It's made once from the icon SVG and committed.

*Why not render previews in the SPA?* Crawlers don't run JavaScript. *Why not a separate preview service?* The API already serves the page, and this is one endpoint.

*Service worker:* it serves the cached shell for navigations in browsers that have it, which is fine: people see the app, and crawlers never have a service worker.

### 5. Rate limiting stays intact
The preview endpoint resolves codes through the same `InviteCodeGuard`, so preview requests count toward the wrong-code limit like the check endpoint, and a blocked IP only ever gets the generic preview. Crawlers fetch real links, so they rarely send wrong codes.

## Risks / Trade-offs

- **Names visible to anyone with the link.** The inviter's display name and the circle name show in the preview and on the join page. That's what the invitee sees after joining anyway, and links expire in 3 days.
- **Preview caching by apps.** WhatsApp caches previews per URL. Every link is new, so stale previews don't matter.
- **`navigator.share` gaps.** Firefox desktop has no share sheet; the WhatsApp, Email and Copy options cover it.
- **3 days may be short** for slow relatives. Generating a new link is one tap, and the lifetime is one constant.

## Migration Plan

No schema change. Deploy as usual. Existing links keep their 7-day expiry; new links get 3 days.
