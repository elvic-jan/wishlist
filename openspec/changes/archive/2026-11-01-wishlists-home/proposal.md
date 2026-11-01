## Why

Most visits are about wishes, not circles, yet Kadoozle opens on "My circles" (GitHub issue #84). Also, the "Circles" menu item only lights up on the circles list, not on a circle's own page, while "Wishlists" stays lit on every wishlist page (GitHub issue #81).

## What Changes

- "My circles" moves from `/` to `/circles`. `/` (and any unknown path) opens `/wishlists`, so the logo, the installed app, sign-in without a redirect and old links all land on Wishlists.
- The web app manifest's `start_url` becomes `/wishlists` (the `id` stays `/`, so installed apps keep working).
- Top menu order: "Wishlists", then "Circles". "Circles" opens `/circles` and is highlighted on `/circles` and on every `/circles/{id}` page (old `/groups/{id}` links redirect there).
- Back on a page opened directly goes to its own section: wishlist pages and Import go to Wishlists, a circle page goes to My circles.
- Links that meant "My circles" now point at `/circles`: leaving a circle, after joining by invite link, "Go to My circles" on an empty Wishlists page, Back on Sign-in & security, and the "invited to a circle" email.
- Release note: "Kadoozle now opens on Wishlists, and the Circles menu stays lit on every circle."

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: new requirements for the default page and the top menu; "Installable as a home-screen app" start URL; "Back returns to the previous screen" goes to the page's own section.
- `user-auth`: "Sign-in redirect stays on the site" falls back to the Wishlists page.

## Impact

- **SPA**: `App.tsx` (routes), `AppShell.tsx` (menu), `BackButton.tsx` (fallback prop), `GroupDetailPage`, `JoinGroupPage`, `SecurityPage`, `WishlistsPage`, `MyWishlistPage`, `MemberWishlistPage`, `ImportPage`, `safeRedirect.ts`, `public/manifest.webmanifest`, `index.html` (release note).
- **API**: `CircleInvitationEmails` link goes to `/circles`.
- No data or API contract changes.
