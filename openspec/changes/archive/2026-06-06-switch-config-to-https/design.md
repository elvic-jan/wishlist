## Context

The wishlist solution is a .NET 9 ASP.NET Core API (`Wishlist.API`) fronting a Vite + React SPA (`Wishlist.Web`). In development, the SPA is served by Vite (default 5173) and forwards `/api` calls to the ASP.NET host via a Vite proxy. Today, four developer-config touchpoints still reference plain HTTP:

| File | Current HTTP usage |
| --- | --- |
| `src/Wishlist.Web/vite.config.ts` | Proxy `target: 'http://localhost:5015'` |
| `src/Wishlist.API/Properties/launchSettings.json` | `http` profile bound to `http://localhost:5015`; `https` profile binds both `https://localhost:7277` and `http://localhost:5015` |
| `src/Wishlist.API/Wishlist.API.csproj` | `<SpaProxyServerUrl>http://localhost:5173</SpaProxyServerUrl>` |
| `src/Wishlist.API/Wishlist.API.http` | `@Wishlist.API_HostAddress = http://localhost:5015` |

ASP.NET Core ships a trusted local dev certificate that can be enabled with `dotnet dev-certs https --trust`. Vite's dev server can also run HTTPS via `@vitejs/plugin-basic-ssl` or a configured cert. There are no production deployment manifests in this repo, so the scope is dev-only configuration.

## Goals / Non-Goals

**Goals:**
- Make HTTPS the only transport used by the dev loop between Vite, the API, and the in-browser SPA.
- Keep the `dotnet run` and `npm run dev` developer workflow unchanged (no new commands required beyond a one-time `dev-certs --trust`).
- Keep `.http` request files working out-of-the-box against the new default scheme.

**Non-Goals:**
- HSTS, certificate pinning, or production TLS configuration (no production config exists in repo yet).
- Changing the chosen port numbers (`7277` for HTTPS API, `5173` for Vite).

**Note:** An earlier draft listed "serving Vite over HTTPS" as a Non-Goal. That was wrong: the API issues the refresh-token cookie with `Secure=true`, which means the browser will neither store nor send it over a plain-HTTP origin — auth would silently break the first time a user reloaded the SPA. Vite-over-HTTPS is therefore now in scope (see Decision 5).

## Decisions

### Decision 1: Use the existing `https` profile's port (`7277`) as the canonical API endpoint

The `launchSettings.json` `https` profile already binds `https://localhost:7277`. We adopt that as the canonical local API URL across all four files rather than introducing a new port.

**Alternatives considered:**
- *Pick a different HTTPS port:* Rejected — no benefit, churns muscle memory, and would require regenerating any locally cached OpenAPI docs.

### Decision 2: Remove the HTTP-only launch profile; keep one `https` profile bound only to `https://localhost:7277`

Today the `https` profile binds both `7277` (HTTPS) and `5015` (HTTP). We drop the `5015` binding so HTTP is not even available, and we delete the standalone `http` profile.

**Alternatives considered:**
- *Keep both bindings, default to HTTPS:* Rejected — leaves an HTTP foot-gun and a stale URL that still appears to work locally, defeating the point of this change.
- *Keep the `http` profile for emergency debugging:* Rejected — `dotnet run --no-https` or a quick local edit covers the rare case; the cost of an always-present HTTP option is permanent drift between dev and prod assumptions.

### Decision 3: Vite proxy targets HTTPS with `secure: false`

Set the proxy target to `https://localhost:7277` and add `secure: false` so the Vite proxy accepts the ASP.NET dev cert (which Node's TLS stack does not trust by default even when the OS does).

**Alternatives considered:**
- *Leave `secure` at default (`true`):* Rejected — Node would reject the self-signed dev cert and break the proxy.
- *Configure Vite to trust the dev cert via `NODE_EXTRA_CA_CERTS`:* Rejected for now — adds developer setup steps; `secure: false` is the standard practice for dev proxies pointing at the ASP.NET dev cert and is scoped strictly to local dev.

### Decision 4: `Wishlist.API.http` uses the canonical HTTPS host address

Update the `@Wishlist.API_HostAddress` variable to `https://localhost:7277` so the file works directly against the new default profile with no per-developer edits.

### Decision 5: Serve the Vite dev server over HTTPS via `@vitejs/plugin-basic-ssl`

Add `@vitejs/plugin-basic-ssl` to the SPA's devDependencies and enable `server.https = true` in `vite.config.ts`. The plugin auto-generates a self-signed cert for `localhost`, no per-developer setup. Also update `<SpaProxyServerUrl>` in the API csproj to `https://localhost:5173` so the .NET SPA proxy launcher matches.

**Why this is required, not optional:** the API issues the refresh-token cookie with `Secure=true` (Decision 3 codified this in spec). Browsers refuse to attach the cookie to responses coming from an HTTP origin and refuse to send it back over HTTP. With Vite serving HTTP and proxying to the HTTPS API, every login would *appear* to succeed but the next page reload would log the user out. Vite-over-HTTPS closes that gap.

**Alternatives considered:**
- *Drop `Secure` from the cookie in dev:* Rejected — directly contradicts the user-auth spec delta this change adds and re-opens the prod/dev gap we're trying to close.
- *Serve the SPA from the .NET host instead of Vite in dev:* Rejected — kills HMR, the main reason for using Vite. Reserved for production builds (already wired up via `BuildSpa` target).
- *Use a manually-issued cert (mkcert, dev-certs --export, etc.):* Rejected for the default workflow — extra per-developer setup. Devs can swap in their own cert via `server.https = { cert, key }` if they want a trusted one.

## Risks / Trade-offs

- **[Risk]** Developers without a trusted ASP.NET dev cert get TLS errors on first run.
  **Mitigation:** Document `dotnet dev-certs https --trust` in `tasks.md` and the change summary. This is a one-time, per-machine step.
- **[Risk]** The `@vitejs/plugin-basic-ssl` cert is self-signed and not in the OS trust store, so the browser shows a "not secure / advanced → proceed" warning on first SPA load.
  **Mitigation:** Accept once per browser profile, or replace with a mkcert-issued cert via `server.https = { cert, key }` if a clean dev experience matters. Document the warning in the change summary so the first dev to pull doesn't think it's a regression.
- **[Risk]** `secure: false` on the Vite proxy is a permissive setting that, if accidentally copy-pasted into a non-dev config, would disable cert validation.
  **Mitigation:** It lives only in `vite.config.ts` which is dev-only; add a brief inline comment scoping it to the local dev cert.
- **[Risk]** External tools/scripts (CI, Postman collections, bookmarks) pointing at `http://localhost:5015` will silently fail to connect after the HTTP binding is removed.
  **Mitigation:** Call this out in the change summary; nothing in-repo currently references the HTTP URL outside the four files being updated, so the blast radius is local-developer-only.
- **[Trade-off]** Dropping the HTTP profile entirely (Decision 2) means a developer who genuinely wants HTTP for a niche debugging case has to edit config or pass flags. We accept this to keep the dev/prod gap small.
