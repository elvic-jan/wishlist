## 1. Icons and manifest

- [x] 1.1 Icon source SVG (white gift on terracotta, inside the maskable safe zone); PNGs at 192, 512 and 180 (`apple-touch-icon`)
- [x] 1.2 `public/manifest.webmanifest` (name, short name, id, start URL, scope, standalone, colours, icons incl. maskable and SVG)
- [x] 1.3 `index.html`: manifest link, light/dark `theme-color`, Apple touch icon, home-screen title and capable tags
- [x] 1.4 API serves `.webmanifest` as `application/manifest+json`

## 2. Install option

- [x] 2.1 Install helper: capture `beforeinstallprompt` early, detect installed (standalone) mode and iPhone/iPad Safari, react to `appinstalled`
- [x] 2.2 "Install app" in the account menu: browser prompt, or Share → Add to Home Screen help on iPhone/iPad; hidden when not applicable
- [x] 2.3 Same option on the sign-in page

## 3. Verification

- [x] 3.1 SPA lint and build; API tests
- [x] 3.2 Built app served by the API: manifest, icons and content types load; service worker active and controlling the page; headless Edge parses the manifest with no errors (its full installability report isn't available headless); with a simulated install offer the sign-in page link appears and opens the browser prompt (the menu item uses the same hook)
- [x] 3.3 Phone-width check of the iPhone help dialog (simulated iPhone user agent)
