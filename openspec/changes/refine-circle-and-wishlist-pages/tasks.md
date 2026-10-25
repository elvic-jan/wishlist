## 1. Circle page

- [x] 1.1 Heading shows the circle name (from `GET /api/groups/me`)
- [x] 1.2 Members: signed-in user first; each member a full-width button with just the name; own row opens `/wishlist/me`, others `/wishlist/:id`
- [x] 1.3 Replace "Other wishlists" with "Other circles" (other circles from `GET /api/groups/me`); stop calling `GET /api/groups/{id}/wishlists`
- [x] 1.4 Loading placeholder until members and circles have loaded

## 2. Wishlist pages

- [x] 2.1 Back-navigation helper: history back when there is a previous screen in the app, otherwise "My circles"; used on both wishlist pages and the circle page
- [x] 2.2 Loading placeholders on My wishlist and a member's wishlist; empty states only after loading

## 3. Sign-in page fix

- [x] 3.0 Background passkey autofill: no error unless the server rejects a chosen passkey; retry up to 3 times (5 s apart) when the request fails to start

## 4. Verification

- [x] 4.1 SPA lint and build
- [x] 4.2 Browser check: circle name, member order and rows, own row opens the editor, other circles; Back returns to the circle page; no "No items yet" flash while loading; direct link Back goes to "My circles"; sign-in page loads without an error; phone width
