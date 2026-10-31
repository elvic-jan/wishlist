## Why

Apps called GiftCircles that do much the same thing already exist on the Play Store and the App Store. People who typed giftcircles.app on Android ended up in store search results and found the other app, and the clash is a trademark risk that only grows with more users. After checking stores, domains and trademark databases, and asking users, the new name is **Kadoozle**. The domain `kadoozle.app` was registered on 2026-10-31 at Cloudflare.

## What Changes

- **Name**: every user-facing "GiftCircles" becomes "Kadoozle": app shell, sign-in pages, browser tab, link previews, home-screen app name, install dialog, update notice, invite messages, error texts and all emails. "Circle" and "wishlist" stay as they are.
- **Icon**: the current gift icon stays for now; a logo of our own is a separate change. Only the link-preview image is redrawn, because it shows the name.
- **Address**: the app moves to `https://www.kadoozle.app` (bare `kadoozle.app` redirects). `giftcircles.app` and `www.giftcircles.app` keep redirecting to the same path on the new address, so old invite links, emails and bookmarks keep working.
- **Email**: emails come from "Kadoozle" <noreply@kadoozle.app>, with SPF, DKIM and DMARC set up for the new domain.
- **Passkeys (clean break, decided 2026-10-31)**: passkeys are tied to a web address, so the ones made for giftcircles.app stop working. They are removed at the move and people add a new one; accounts without a password get in through the "Can't sign in?" email. There are only a few users with passkeys so far.
- **Telling users**: a one-time message after the move explains the new name and address and offers to add a passkey. The release note announces the new name.
- **Infra**: the container app can bind a second web address, so the old one keeps working while the new certificate is issued (no downtime).

## Impact on users

- Everyone is signed out once (sign-ins are stored per web address) and signs in again on kadoozle.app.
- Passkeys need to be added again.
- Home-screen installs of GiftCircles need to be removed and Kadoozle installed again.
- Old links keep working through the redirect.

## Non-goals

- No renaming in code, namespaces (`Wishlist.*`), API routes, database names, Azure resource names or browser storage keys.
- No change to behaviour, permissions or data, apart from removing the old passkeys.
- No new logo or app icons (separate change), and no BIMI logo in mail clients yet.

## Capabilities

### Modified Capabilities
- `ui-foundations`: product name, emails, home-screen app and update notice say Kadoozle; message about the move.
- `groups`: invite link preview says Kadoozle.
- `hosting`: security-header scenario wording; new requirement for the address and the old-address redirect.
- `user-auth`: new requirement that passkeys belong to kadoozle.app.

## Impact

- **Web**: `index.html`, `manifest.webmanifest`, `AppShell`, `AuthLayout`, `InstallApp`, `UpdateToaster`, `inviteMessage`, `webauthn`, `LoginPage`, `SecurityPage`, `ConfirmEmailPage`; link-preview image `public/og/kadoozle.png` (same icon, new name).
- **API / Application**: link preview text in `Program.cs`; all email texts; `Messages.cs` (new move message, legacy import text); a migration that deletes old passkeys.
- **Config / infra**: `Smtp:FromName`, `containerapp.bicep` (second custom domain), `email.bicep` sender name; GitHub `production` variables `CUSTOM_DOMAIN`, `EMAIL_DOMAIN`, `PUBLIC_SITE_URL`; `deploy.yml` fallback URL; CI warning text; `infra/README.md`.
- **Outside the repo**: Cloudflare DNS and redirect rules for both domains, Azure managed certificate, ACS email domain verification.
- **Tests**: tests that assert on "GiftCircles" texts.
