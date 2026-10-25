## Why

The app now lives at `giftcircles.app`, but it still calls itself "Wishlist" and talks about "groups". "Circle" fits how people actually use it (the family circle, the cousins circle), and one consistent name across the domain, the UI and the emails avoids confusing relatives who receive an invite.

## What Changes

- The product name shown to users becomes **GiftCircles**: app shell and sign-in header, the browser tab title, the email sender name, and the subject and body of the password-reset and password-changed emails.
- Everywhere the UI says "group", it says "circle" instead: navigation ("Circles"), the home page ("My circles", "New circle", "Create circle", "Circle name"), the circle page, joining and leaving, sign-in and sign-up hints.
- API error messages that users can see say "circle" instead of "group" (for example "You are not a member of this circle.").
- The circle page moves from `/groups/:id` to `/circles/:id`. The old address redirects to the new one so bookmarks keep working. Invite links (`/join/:code`) do not change.
- "Wishlist" stays as the word for a person's list of wishes ("My wishlist", "View wishlist").

## Non-goals

- No renaming in code, API routes (`/api/groups/...`), database tables or OpenSpec capability names. `Group` in code means "circle" in the UI.
- No change to behaviour, permissions or data.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: adds a requirement fixing the product name and the user-facing word for a group.

## Impact

- **Web**: `AppShell`, `AuthLayout`, `index.html`, `App.tsx` routes, and the home, circle, join, sign-in, sign-up and my-wishlist pages.
- **Application**: exception messages in the group, wishlist and gift-claim use cases; the two password emails.
- **Config**: `Smtp:FromName` default in `appsettings.json` and the `smtpFromName` default in `infra/modules/containerapp.bicep` become `GiftCircles`. Production picks the new sender name up on the next `infra` run.
- No migration, no API contract change.
