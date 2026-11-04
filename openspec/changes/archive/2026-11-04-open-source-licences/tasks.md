## 1. Licence list

- [x] 1.1 Vite plugin `build/licenses.ts`: from the bundle's modules, find each npm package, read its `package.json` and licence file, emit `licenses.json` (sorted by name); fail the build when a bundled package has no licence at all
- [x] 1.2 `/licenses.json` served with `Cache-Control: no-cache` (`SpaPage.CacheControlFor`), with a unit test

## 2. Page

- [x] 2.1 `LicensesPage` at `/licenses` (signed in or not): thanks, Lucide named for logo and icons, every package with version, licence and homepage link, expandable full licence text; loading skeleton; message when the list can't be loaded
- [x] 2.2 Account menu item "Open-source licences" under "What's new"
- [x] 2.3 Release note `release-notes/open-source-licences.md` (fix)

## 3. Verification

- [x] 3.1 SPA lint and build; `dist/licenses.json` lists lucide-react (ISC), react (MIT) and the other bundled packages with licence text
- [x] 3.2 API tests pass
- [x] 3.3 Browser check on the built app: menu item opens the page, Lucide shown, a licence text expands; page works signed out
