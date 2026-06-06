## 1. Prerequisites

- [ ] 1.1 Run `dotnet dev-certs https --trust` and confirm the local ASP.NET Core dev cert is trusted by the OS keychain
- [x] 1.2 Confirm port `7277` (HTTPS API) is free on the dev machine

## 2. API host configuration

- [x] 2.1 In `src/Wishlist.API/Properties/launchSettings.json`, delete the `http` profile entirely
- [x] 2.2 In the same file, change the `https` profile's `applicationUrl` to only bind `https://localhost:7277` (remove the trailing `;http://localhost:5015`)
- [x] 2.3 In `src/Wishlist.API/Wishlist.API.csproj`, update `<SpaProxyServerUrl>` from `http://localhost:5173` to `https://localhost:5173` — the SPA now serves HTTPS too (see section 6). (Earlier draft of this change incorrectly left it on HTTP; corrected in scope expansion.)
- [x] 2.4 In `src/Wishlist.API/Wishlist.API.http`, change `@Wishlist.API_HostAddress` from `http://localhost:5015` to `https://localhost:7277` (also retargeted the sample request to `GET /api/auth/me` because the stale `/weatherforecast` endpoint no longer exists in this codebase)

## 3. API code: enforce HTTPS-only for auth endpoints

- [x] 3.1 In the API startup (`Program.cs`), ensure `app.UseHttpsRedirection()` is registered before authentication middleware so any stray HTTP request is upgraded
- [x] 3.2 In the refresh-token cookie configuration, set `CookieOptions.Secure = true` unconditionally (no environment branch) and confirm `HttpOnly = true` is already set
- [x] 3.3 Add an explicit `[RequireHttps]` attribute (or equivalent endpoint-filter) on the auth endpoint group covering register, login, refresh, and logout so HTTP requests get a 400/redirect even if middleware ordering changes

## 4. Vite proxy configuration

- [x] 4.1 In `src/Wishlist.Web/vite.config.ts`, change the `/api` proxy `target` to `https://localhost:7277`
- [x] 4.2 In the same proxy block, add `secure: false` with a brief inline comment noting it is required to accept the ASP.NET Core local dev cert

## 5. Verification

- [x] 5.1 Start the API with `dotnet run --project src/Wishlist.API` and verify it binds only `https://localhost:7277` (no HTTP listener in the console output) — verified: API log shows `Now listening on: https://localhost:7277` (single binding, HTTPS only).
- [x] 5.2 Start the SPA with `npm run dev` in `src/Wishlist.Web` and confirm `/api/*` requests succeed in the browser network tab over the Vite proxy — verified via curl: `http://localhost:5173/api/auth/me` → 401 (proxy successfully forwards to HTTPS API with `secure: false`).
- [x] 5.3 Open `src/Wishlist.API/Wishlist.API.http` in the IDE and execute the sample request; confirm 200 OK over HTTPS — verified via curl on the same URL: `GET https://localhost:7277/api/auth/me` → 401 over HTTPS (handshake + auth pipeline reachable; a 401 from a protected endpoint is the expected "responsive over HTTPS" outcome).
- [x] 5.4 Attempt a direct `curl http://localhost:5015/...` request and confirm it fails to connect (no HTTP listener bound) — verified: `curl: (7) Failed to connect to localhost port 5015`. Also confirmed no plain-HTTP listener on 7277 (`curl: (52) Empty reply from server`).
- [x] 5.5 Log in via the SPA and inspect the refresh token cookie in DevTools — confirm `Secure`, `HttpOnly`, and `SameSite` attributes are all set — verified via curl after Postgres came up: `POST /api/auth/login` returned `Set-Cookie: refreshToken=...; path=/; secure; samesite=strict; httponly`. All three attributes present.

## 6. SPA dev server over HTTPS (scope addition)

This section was added after the initial implementation when it became clear that serving Vite over HTTP while the API issues `Secure` cookies silently breaks the auth flow (browser refuses to attach `Secure` cookies to an HTTP origin). See design.md Decision 5.

- [x] 6.1 In `src/Wishlist.Web`, install `@vitejs/plugin-basic-ssl` as a devDependency
- [x] 6.2 In `src/Wishlist.Web/vite.config.ts`, import the plugin, add it to `plugins`, and set `server.https = true`
- [x] 6.3 Restart `npm run dev` and verify the startup banner shows `Local: https://localhost:5173/`
- [x] 6.4 Verify `curl -k https://localhost:5173/` returns 200 (index.html served over HTTPS)
- [x] 6.5 Verify the proxied API path still works under HTTPS: `curl -k https://localhost:5173/api/auth/me` returns 401
- [x] 6.6 Verify plain HTTP to Vite no longer responds: `curl http://localhost:5173/` fails the TLS handshake (no plain-HTTP listener)

## 7. Cleanup

- [x] 7.1 Search the repo for any remaining references to `http://localhost:5015` and either remove them or convert them to `https://localhost:7277`
- [x] 7.2 Update the project root README (if it documents the API URL) to reference the new HTTPS URL and the `dev-certs --trust` prerequisite — N/A: current `README.md` does not document any URLs, so nothing to update.
