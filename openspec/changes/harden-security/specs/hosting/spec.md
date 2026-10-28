## ADDED Requirements

### Requirement: Security headers on every response
Every response from the application, including the SPA page, static files, API responses and errors, SHALL carry these headers:
- `Content-Security-Policy` that allows scripts, styles, images, fonts, connections, the manifest and the service worker only from the site itself (plus `data:` images and inline styles), allows inline scripts only by the hash of the SPA page's own inline scripts, and sets `object-src 'none'`, `base-uri 'self'`, `form-action 'self'` and `frame-ancestors 'none'`
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` that turns off camera, microphone, geolocation and payment

Outside local development, HTTPS responses SHALL also carry `Strict-Transport-Security` with a max-age of one year.

#### Scenario: Page cannot be framed
- **WHEN** another site embeds a GiftCircles page in a frame
- **THEN** the browser refuses to show it

#### Scenario: Only the app's own inline script runs
- **WHEN** the SPA page is served
- **THEN** its Content-Security-Policy lists the SHA-256 hash of each inline script in that page, and no other inline script would run

#### Scenario: API responses carry the headers too
- **WHEN** any API endpoint responds, including with an error
- **THEN** the response carries the same security headers

#### Scenario: HSTS when deployed
- **WHEN** a deployed instance answers an HTTPS request
- **THEN** the response carries `Strict-Transport-Security: max-age=31536000`
