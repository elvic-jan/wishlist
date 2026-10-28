# hosting Specification

## Purpose
TBD - created by archiving change add-cheap-azure-hosting. Update Purpose after archive.
## Requirements
### Requirement: Single container serves SPA and API from one origin

The system SHALL be packaged as one container image in which the ASP.NET Core API serves both the `/api` endpoints and the built SPA bundle, so the browser loads the SPA and calls the API on the same HTTPS origin.

#### Scenario: SPA route is served by the API container

- **WHEN** a browser requests `/` or any client-side route such as `/wishlists/123` from the deployed origin
- **THEN** the container responds with the SPA's `index.html` and its static assets are served from the same origin

#### Scenario: API call stays same-origin

- **WHEN** the SPA calls `/api/auth/refresh` on the deployed origin
- **THEN** the request is handled by the API in the same container and the `Secure; HttpOnly; SameSite=Strict` refresh cookie is sent and accepted without any cross-origin configuration

### Requirement: Compute scales to zero and is capped at one replica

The deployed app SHALL scale to zero replicas when idle and SHALL never run more than one replica, so idle cost is zero and in-process state (the forgot-password rate limiter) stays correct.

#### Scenario: Idle app scales to zero

- **WHEN** the app receives no requests for the platform's scale-down window
- **THEN** the number of running replicas drops to zero

#### Scenario: Load does not create a second replica

- **WHEN** concurrent request volume would otherwise trigger scale-out
- **THEN** the app continues to run on at most one replica

### Requirement: Health endpoint independent of the database

The system SHALL expose an unauthenticated `GET /healthz` endpoint that returns 200 when the process is able to serve requests, without querying the database.

#### Scenario: Health probe succeeds without a database round-trip

- **WHEN** the platform or CI calls `GET /healthz`
- **THEN** the API returns 200 OK and issues no database query

### Requirement: Deployed configuration comes from environment variables and platform secrets

In deployed environments the system SHALL take all environment-specific settings from environment variables, and SHALL take secrets (database connection string, JWT signing key, SMTP credentials) only from platform-managed secrets referenced by those environment variables. Secrets SHALL NOT be stored in the image, in `appsettings*.json`, or in the repository.

#### Scenario: Secret value is not present in the image

- **WHEN** the production image is inspected
- **THEN** it contains no production connection string, JWT key, or SMTP password

#### Scenario: Missing required setting fails startup

- **WHEN** the app starts in the `Production` environment without `Smtp:Host` or with a non-HTTPS `App:PublicBaseUrl`
- **THEN** startup fails with a configuration error and the platform does not route traffic to that revision

### Requirement: Data Protection keys survive restarts

The system SHALL persist ASP.NET Core Data Protection keys in the application database so that tokens protected before a container restart (including password-reset tokens) remain valid after it.

#### Scenario: Reset link survives a scale-to-zero restart

- **WHEN** a user requests a password reset, the app scales to zero and later starts a new replica, and the user opens the reset link within its 30-minute lifetime
- **THEN** the token is accepted and the password can be reset

### Requirement: Production deploys are gated and migrate the schema first

The system SHALL deploy to production only from the `main` branch through CI, only after a human approves the deployment, and SHALL apply pending EF Core migrations to the production database before switching traffic to the new image. Images SHALL be referenced by an immutable commit-derived tag.

#### Scenario: Deploy waits for approval

- **WHEN** a commit lands on `main` and the image build succeeds
- **THEN** the production deploy job does not run until an approver approves it in the `production` GitHub environment

#### Scenario: Migrations run before the new image takes traffic

- **WHEN** an approved deploy contains a new migration
- **THEN** the migration is applied to the production database and only afterwards is the container app updated to the new `sha-` image tag

#### Scenario: Failed migration aborts the deploy

- **WHEN** applying migrations fails
- **THEN** the container app keeps running the previous image and the workflow run fails

#### Scenario: Temporary database access is always revoked

- **WHEN** the deploy job finishes, whether successfully or not
- **THEN** the firewall rule it opened for the CI runner is removed

### Requirement: CI authenticates to Azure without stored credentials

CI SHALL authenticate to Azure using OpenID Connect federated credentials scoped to the production GitHub environment, and SHALL NOT store any Azure client secret or password in GitHub.

#### Scenario: Workflow outside the production environment cannot deploy

- **WHEN** a workflow job that is not bound to the `production` environment attempts to log in to Azure
- **THEN** the token exchange is rejected and no Azure resource can be changed

### Requirement: Monthly cost is capped and monitored

The infrastructure SHALL be sized so that expected monthly cost stays under $25 at family-scale usage, and SHALL include a budget alert on the resource group that notifies the owner when actual spend reaches 80% and 100% of $25.

#### Scenario: Spend crosses the alert threshold

- **WHEN** actual spend for the resource group in the current month reaches $20
- **THEN** the configured recipient receives a budget alert email

### Requirement: Monthly reservation job
A scheduled job SHALL run on the 1st of every month at 07:00 UTC, in the same Container Apps environment and from the same image as the app, to send reservation reminders, end expired reservations and email their reservers. It SHALL use the app's database and email settings, need no public endpoint, and be updated to the new image on every deploy.

#### Scenario: Runs monthly
- **WHEN** it is 07:00 UTC on the 1st of a month
- **THEN** the job starts, processes reservations, logs how many reminders and expiries it handled, and exits

#### Scenario: Same code as the app
- **WHEN** a new version is deployed
- **THEN** the job's image is updated to the same image as the app

#### Scenario: Runs without the app being awake
- **WHEN** the app is scaled to zero at the scheduled time
- **THEN** the job still runs, without starting the web app

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

