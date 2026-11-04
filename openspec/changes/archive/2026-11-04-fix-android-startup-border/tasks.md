## 1. Startup screen

- [x] 1.1 Manifest `background_color` to `#9a3412`; `theme_color` stays `#c2410c`
- [x] 1.2 Rounded-corner "any" icons: `icon.svg`, `icon-192.png`, `icon-512.png` (transparent corners, anti-aliased)
- [x] 1.3 Full-bleed maskable icon in its own file `icon-maskable-512.png`, referenced by the manifest's maskable entry
- [x] 1.4 Release note `release-notes/fix-android-startup-border.md` (fix): "Cleaner startup screen on Android"

## 2. Verification

- [x] 2.1 SPA lint and build; manifest and all icons served by the dev server
- [x] 2.2 Pixel check: maskable and apple-touch icons filled to every edge; "any" icons transparent in the corners and terracotta along the middle of each side
- [ ] 2.3 On an Android phone after deploy (user): startup screen shows the icon on the darker background, no faint border; navigation bar still coloured in dark mode
