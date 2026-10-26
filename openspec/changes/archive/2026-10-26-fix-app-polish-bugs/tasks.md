## 1. iPhone icon

- [x] 1.1 Fill the white edge strip of `apple-touch-icon.png` and the faint edge of `icon-192.png` with the terracotta background; check every edge pixel of all icon PNGs

## 2. "My circles" loading state

- [x] 2.1 `HomePage` shows a loading placeholder until `GET /api/groups/me` has answered; "No circles yet" only for an empty loaded list; on a load error no empty state

## 3. Android system bars

- [x] 3.1 Manifest `theme_color` and `background_color` set to terracotta `#c2410c`
- [x] 3.2 `theme-color` meta tags follow the resolved in-app mode (first-paint script and `ThemeProvider`)
- [x] 3.3 Declare `color-scheme` (light / dark) on `<html>`

## 4. Verification

- [x] 4.1 SPA lint and build
- [x] 4.2 Browser check: no "No circles yet" flash on "My circles" (slow network), empty state still shown for a user with no circles, theme-color follows the mode toggle, manifest serves the new colours
- [x] 4.3 On a phone after deploy (user): iPhone icon re-added has no white line; Android navigation bar no longer white in dark mode
