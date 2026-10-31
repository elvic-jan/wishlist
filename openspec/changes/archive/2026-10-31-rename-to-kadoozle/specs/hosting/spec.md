## MODIFIED Requirements

### Requirement: Security headers on every response
Every response from the application, including the SPA page, static files, API responses and errors, SHALL carry these headers:
- `Content-Security-Policy` that allows scripts, styles, images, fonts, connections, the manifest and the service worker only from the site itself (plus `data:` images and inline styles), allows inline scripts only by the hash of the SPA page's own inline scripts, and sets `object-src 'none'`, `base-uri 'self'`, `form-action 'self'` and `frame-ancestors 'none'`
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` that turns off camera, microphone, geolocation and payment

Outside local development, HTTPS responses SHALL also carry `Strict-Transport-Security` with a max-age of one year.

#### Scenario: Page cannot be framed
- **WHEN** another site embeds a Kadoozle page in a frame
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

## ADDED Requirements

### Requirement: The app lives at www.kadoozle.app and the old address forwards
The application SHALL be served at `https://www.kadoozle.app`, with `kadoozle.app` redirecting there. The former addresses `giftcircles.app` and `www.giftcircles.app` SHALL keep redirecting permanently (301) to the same path and query on `https://www.kadoozle.app`, and the `giftcircles.app` domain SHALL stay registered, so links in earlier emails, shared invite links and bookmarks keep working.

#### Scenario: New address
- **WHEN** someone opens `https://kadoozle.app/some/path?x=1`
- **THEN** they are redirected to `https://www.kadoozle.app/some/path?x=1` and the app loads with a valid certificate

#### Scenario: Old invite link
- **WHEN** someone opens an invite link `https://www.giftcircles.app/join/{code}` shared before the move
- **THEN** they are redirected to `https://www.kadoozle.app/join/{code}` and can join as before

#### Scenario: Old bare domain
- **WHEN** someone opens `https://giftcircles.app/`
- **THEN** they end up on `https://www.kadoozle.app/`

#### Scenario: No downtime during the switch
- **WHEN** the new address is being set up and its certificate issued
- **THEN** the app stays reachable at `www.giftcircles.app` until `www.kadoozle.app` works
