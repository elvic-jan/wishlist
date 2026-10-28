## ADDED Requirements

### Requirement: Cache headers let new releases through
The system SHALL send the SPA page and the service worker script with `Cache-Control: no-cache`, and the content-hashed files under `/assets/` with `Cache-Control: public, max-age=31536000, immutable`. Personal pages such as `/join/{code}` SHALL stay `no-store`.

#### Scenario: Page always revalidated
- **WHEN** a browser requests `/index.html` or any SPA route such as `/circles/123`
- **THEN** the response has `Cache-Control: no-cache`

#### Scenario: Service worker always revalidated
- **WHEN** a browser requests `/sw.js`
- **THEN** the response has `Cache-Control: no-cache`

#### Scenario: Hashed assets cached for good
- **WHEN** a browser requests a file under `/assets/`
- **THEN** the response has `Cache-Control: public, max-age=31536000, immutable`

#### Scenario: Invite page never stored
- **WHEN** a browser requests `/join/{code}`
- **THEN** the response has `Cache-Control: no-store`
