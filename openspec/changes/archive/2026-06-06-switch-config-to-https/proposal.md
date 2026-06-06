## Why

The wishlist app currently has multiple development configuration entries that use plain `http://` URLs (API launch profile, Vite dev proxy target, `.http` test file, and the SPA proxy URL in the API csproj). Standardizing local development on HTTPS reduces the gap between dev and production behavior, avoids mixed-content surprises when adding authenticated or BFF flows later, and aligns with the project's clean-architecture/BFF direction.

- Switch the Vite dev proxy in `src/Wishlist.Web/vite.config.ts` to target the API's HTTPS endpoint instead of HTTP.
- Serve the Vite dev server itself over HTTPS (`https://localhost:5173`) by adding the `@vitejs/plugin-basic-ssl` plugin, so the browser will accept and send the API's `Secure`-flagged refresh-token cookie in dev.
- Make `https` the only profile in `src/Wishlist.API/Properties/launchSettings.json` and remove the standalone `http`-only profile.
- Update the SPA proxy URL in `src/Wishlist.API/Wishlist.API.csproj` (`SpaProxyServerUrl`) to use HTTPS for the Vite dev server.
- Update `src/Wishlist.API/Wishlist.API.http` to call the API via its HTTPS host address.
- **BREAKING (dev only)**: Local developers must trust the ASP.NET Core dev certificate (`dotnet dev-certs https --trust`) and accept the basic-ssl self-signed cert on first SPA load. Update any bookmarks/scripts that still point at the HTTP URLs.

## Capabilities

### New Capabilities

_None. This change does not introduce a new product capability._

### Modified Capabilities

- `user-auth`: Add a requirement that authentication endpoints (login, refresh, logout) and the HttpOnly refresh token cookie SHALL only be served over HTTPS, including in local development. This codifies the security intent behind the config switch so future config edits can't silently re-introduce plain HTTP for token traffic.

## Impact

- **Affected code**:
  - `src/Wishlist.Web/vite.config.ts`
  - `src/Wishlist.Web/package.json` (adds `@vitejs/plugin-basic-ssl` devDependency)
  - `src/Wishlist.API/Properties/launchSettings.json`
  - `src/Wishlist.API/Wishlist.API.csproj`
  - `src/Wishlist.API/Wishlist.API.http`
  - `src/Wishlist.API/Controllers/AuthController.cs` (`[RequireHttps]` on the auth route group)
- **APIs**: No public API contract changes; only the dev transport scheme changes.
- **Dependencies**: Relies on the existing ASP.NET Core HTTPS dev certificate; adds `@vitejs/plugin-basic-ssl` (devDependency only).
- **Developer workflow**: Devs must have the dev cert trusted; Vite must accept the dev cert when proxying (`secure: false` on the proxy); browser will show a "not secure" warning on first SPA load until the basic-ssl cert is accepted.
