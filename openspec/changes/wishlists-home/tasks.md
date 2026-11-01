## 1. Routes and menu

- [x] 1.1 `App.tsx`: My circles at `/circles`; `/` and unknown paths redirect to `/wishlists`
- [x] 1.2 `AppShell.tsx`: logo opens `/wishlists`; menu order Wishlists, Circles; "Circles" opens `/circles` and is highlighted on `/circles` and `/circles/{id}` (like `inWishlists`)
- [x] 1.3 `manifest.webmanifest`: `start_url` `/wishlists` (keep `id`)
- [x] 1.4 `safeRedirect.ts`: fall back to `/wishlists`

## 2. Links that meant My circles

- [x] 2.1 `BackButton`: fallback target as a prop; wishlist pages and Import go to `/wishlists`, circle page to `/circles`
- [x] 2.2 Leave circle, join by invite link, "Go to My circles", Security page Back go to `/circles`
- [x] 2.3 `CircleInvitationEmails`: link to `/circles`

## 3. Release and checks

- [x] 3.1 Release note in `index.html`: "Kadoozle now opens on Wishlists, and the Circles menu stays lit on every circle." (confirmed with the user)
- [x] 3.2 API tests, SPA lint and build pass
- [ ] 3.3 Browser check: logo, `/`, menu order and highlighting on `/circles` and `/circles/{id}` (browser pane rejects the dev certificate; check after deploy)
