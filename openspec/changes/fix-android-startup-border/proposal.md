## Why

Issue #83: the Android startup screen of the installed app shows a faint border around the logo. On Android 12 and newer the system builds that screen from the home-screen icon, cut to the launcher's rounded shape, on the manifest's `background_color`. Icon and background are the same terracotta, so only the soft edge of the cut shows, as a faint outline (seen on a Xiaomi phone). The icon images themselves are clean.

## What Changes

- Manifest `background_color` becomes a deeper terracotta (`#9a3412`), so the orange icon sits on it with a crisp, intended edge. `theme_color` stays `#c2410c`, so the Android navigation bar keeps its colour.
- The "any" icons (`icon-192.png`, `icon-512.png`, `icon.svg`) get rounded corners with transparent corners, so Android 11 and older (Chrome's own startup screen) and desktop installs show a rounded tile instead of a sharp square on the darker background.
- The maskable icon becomes its own full-bleed file (`icon-maskable-512.png`), so the home-screen icon is unchanged. `apple-touch-icon.png` stays full-bleed (iPhone rounds it itself).
- Release note (fix): "Cleaner startup screen on Android".

No effect on iPhone: Safari doesn't use the manifest's `background_color` for its launch screen.

## Impact

- `src/Wishlist.Web/public/manifest.webmanifest`, `public/icons/*`
- Spec `ui-foundations`: "Installable as a home-screen app" (icon edge and startup screen scenarios)
- Installed apps pick up the change when Chrome refreshes them (can take a few days; reinstalling shows it at once).
