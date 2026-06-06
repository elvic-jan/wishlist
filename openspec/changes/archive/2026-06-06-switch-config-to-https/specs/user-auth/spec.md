## ADDED Requirements

### Requirement: Authentication traffic served only over HTTPS

The system SHALL serve all authentication endpoints — registration, login, token refresh, and logout — exclusively over HTTPS in every environment, including local development. The HttpOnly refresh token cookie SHALL be issued with the `Secure` attribute so that browsers refuse to transmit it over plain HTTP.

#### Scenario: HTTP request to an auth endpoint is refused

- **WHEN** a client sends a request to a registration, login, refresh, or logout endpoint over plain HTTP
- **THEN** the API does not process the credentials or token, and the request fails to connect (the API does not bind an HTTP listener for these endpoints in any environment, including local development)

#### Scenario: Refresh token cookie cannot leak over HTTP

- **WHEN** the API issues the HttpOnly refresh token cookie after a successful registration, login, or refresh
- **THEN** the cookie carries the `Secure` attribute so the browser only sends it back over HTTPS, and never over an HTTP request to the same host

### Requirement: SPA host that consumes auth cookies SHALL itself be served over HTTPS

To keep the `Secure`-flagged refresh-token cookie usable, any origin that issues authenticated requests against the API SHALL itself be served over HTTPS in every environment, including the local Vite dev server. The system SHALL NOT ship a default developer configuration where the SPA is served over plain HTTP while the API issues `Secure` cookies, because the browser will silently drop the cookie and break the user's session on the next reload.

#### Scenario: Local Vite dev server serves HTTPS

- **WHEN** a developer runs `npm run dev` for the SPA
- **THEN** the dev server binds an HTTPS listener (default `https://localhost:5173`) so the browser treats the SPA origin as secure and will store/send the API's `Secure` refresh-token cookie

#### Scenario: Browser receives Secure cookie via a secure origin

- **WHEN** a user logs in through the SPA and the `Set-Cookie: refreshToken=...; Secure; HttpOnly; SameSite=Strict` response is delivered through the dev proxy
- **THEN** the browser stores the cookie (because the response origin is HTTPS) and sends it on the next `POST /api/auth/refresh` request, so the session survives a page reload
