## 1. Web app

- [x] 1.1 Show "GiftCircles" in `AppShell`, `AuthLayout` and the `index.html` title
- [x] 1.2 Navigation "Groups" → "Circles"
- [x] 1.3 Replace "group" with "circle" in the home, circle detail, join, sign-in, sign-up and my-wishlist pages (headings, empty states, buttons, dialogs, error fallbacks)
- [x] 1.4 Route the circle page at `/circles/:groupId`, redirect `/groups/:groupId` to it, and update in-app links and post-sign-up navigation

## 2. API and emails

- [x] 2.1 Use "circle" in user-visible exception messages of the group, wishlist and gift-claim use cases
- [x] 2.2 Use "GiftCircles" in the password-reset and password-changed emails
- [x] 2.3 Default email sender name "GiftCircles" in `appsettings.json` and `infra/modules/containerapp.bicep`

## 3. Verification

- [x] 3.1 API tests, SPA lint and build pass
- [x] 3.2 No user-visible "group" or "Wishlist" (as the app name) left in the web app, messages or emails
- [x] 3.3 Local check in the browser: shell, home, circle page, old `/groups/:id` redirect (join and sign-up pages checked by text)
