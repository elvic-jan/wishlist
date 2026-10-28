## Why

After a release, people keep using the previous version of the app until they refresh by hand. The service worker shows the cached page instantly (so the app opens during a cold start) and only fetches the new page in the background, so the new version arrives one visit late. The server is already new, so its messages can point at features the old app doesn't have. After the buddies release, the "Pick my buddies" action opened My wishlist without the buddies dialog or button; only a manual refresh fixed it.

## What Changes

- **New version notice:** the app checks whether a newer version is deployed when it starts and whenever it comes back to the foreground. If there is one, it shows a lasting toast in the same style as the what's-new messages: "New version available" with **Refresh** and **Not now**. Refresh loads the new version. The app never reloads by itself.
- **Message actions land in the new version:** when a message's action is used while a newer version is deployed, the app loads the new version straight onto the action's page instead of navigating inside the old one.
- **Refresh really gets the new version:** before reloading, the app asks the service worker to fetch the new page first, so the reload doesn't show the old cached page again.
- **Cache headers:** the page itself (`index.html` and every SPA route) and `sw.js` are sent with `Cache-Control: no-cache` (always check with the server). The content-hashed files under `/assets/` are sent as cacheable for a year and immutable. The personal `/join/{code}` page stays `no-store`.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `ui-foundations`: new requirement that the app picks up new versions after a release.
- `messages`: a message's action loads the new version when one is deployed.
- `hosting`: cache headers for the page, the service worker and the hashed assets.

## Impact

- **Web**: `public/sw.js`, new `lib/appUpdate.ts`, new `UpdateToaster`, shared toast card used by `MessageToaster`, `main.tsx`, `App.tsx`.
- **API**: `Program.cs` static file and fallback headers, `SpaPage`.
- **Existing installs**: the first release with this change still arrives one visit late (the old app doesn't know how to check). From then on the notice appears.
