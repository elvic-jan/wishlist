## Why

Three small bugs make the installed app look unfinished:

- On iPhone the home-screen icon has a thin white line along its left edge.
- Going back from a circle to "My circles" briefly shows "No circles yet" with the orange "New circle" button before the circles appear.
- On Android in dark mode, the system navigation bar (the on-screen buttons) of the installed app stays light.

## What Changes

- **iPhone icon:** the `apple-touch-icon` PNG had a 3-pixel white strip on its left edge (and the 192 px icon a faint 1-pixel one). The icons are corrected so their background is terracotta right to the edge.
- **"My circles" loading:** the page shows a loading placeholder until the circles have loaded, and the "No circles yet" card only when the user really has no circles. This is the existing "Lists don't show an empty state before loading" rule, which the page was missing.
- **Android navigation bar:** Chrome colours an installed app's navigation bar from the manifest's fixed `theme_color` / `background_color`, which were both near-white, so the bar stayed white in dark mode. Both become the brand terracotta (the icon's background), which has contrast with the light system buttons in light and dark mode. As a side effect the launch screen is terracotta with the white gift.
- **Browser bars follow the in-app mode:** the `theme-color` meta tags only followed the phone's setting. They now also follow the mode chosen in the app (Light / Dark / System), and the page declares its `color-scheme` so the browser's own controls match.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: the installable-app requirement covers the icon edges and the system bars; the loading-state requirement covers "My circles".

## Impact

- **Web**: `HomePage`, `ThemeContext`, `index.html` first-paint script, `index.css`, `manifest.webmanifest`, the icon PNGs.
- **API**: none.
- **Existing installs**: Android picks up the new manifest colours when Chrome updates the installed app (this can take a day), or after reinstalling. iPhone only reads the icon when the app is added to the home screen, so it needs to be removed and added again.
