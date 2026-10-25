## Why

Relatives mostly use GiftCircles on their phones. Opening it from a browser tab means finding the link again each time, and it looks like a website rather than an app. Installing it to the home screen gives an icon, a full-screen window and a faster start, with no app store. The app already has a service worker that renders the shell instantly; it only lacks the manifest, icons and an install path.

## What Changes

- A web app manifest (`/manifest.webmanifest`): name "GiftCircles", standalone display, start at `/`, colours matching the light theme, and icons (192 and 512 px, maskable, plus an SVG).
- App icons: the gift symbol in white on terracotta, kept inside the maskable safe zone so Android's icon shapes never crop it; a 180 px `apple-touch-icon` for iPhone and iPad.
- Page head: manifest link, light/dark `theme-color`, Apple home-screen title and full-screen tags.
- "Install app" in the account menu:
  - Android and desktop Chrome/Edge: opens the browser's own install dialog.
  - iPhone and iPad (Safari has no install prompt): shows how to add it with Share → Add to Home Screen.
  - Hidden when the app is already running installed, or when the browser can't install it.
- The sign-in page offers the same "Install app" link, since many people open their invite on a phone before having an account.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: adds a requirement that the app is installable as a home-screen app.

## Impact

- **Web**: `index.html`, `public/manifest.webmanifest`, `public/icons/*`, a small install helper, the account menu and sign-in page.
- **API**: serves `.webmanifest` with the `application/manifest+json` content type (explicit mapping, so static files never refuse it).
- No backend logic or database change. Passkeys keep working inside the installed app (same origin and RP ID).
