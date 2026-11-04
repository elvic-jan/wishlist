## Why

Issue #82: the gift logo and all UI icons come from Lucide (ISC), and the other libraries bundled into the web app (React, React Router, Radix UI, axios, …) are MIT-licensed. Both licences ask that their copyright and licence notice travel with copies, and the app ships those libraries to every browser without naming them.

## What Changes

- A build step (Vite plugin) collects every npm package that ends up in the browser bundle and writes `/licenses.json`: name, version, licence, homepage and the full licence text. It follows the bundle, so it stays correct when libraries are added or removed.
- New page "Open-source licences" at `/licenses`, open to anyone (signed in or not), linked from the account menu under "What's new". It thanks the projects, names Lucide as the source of the logo and icons, and lists every package with its licence; each entry expands to its full licence text.
- `/licenses.json` is served `no-cache`, like `/releases.json`, so a release brings the new list.
- Release note (fix): "New Open-source licences page crediting the software Kadoozle is built with".

## Impact

- `src/Wishlist.Web/vite.config.ts`, new `src/Wishlist.Web/build/licenses.ts`
- New page `src/Wishlist.Web/src/pages/LicensesPage.tsx`, route in `App.tsx`, menu item in `AppShell.tsx`
- `src/Wishlist.API/SpaPage.cs` (cache header)
- Spec `ui-foundations`: new requirement "Open-source licences are credited"
- Server-side .NET packages are not distributed to users and are not listed.
