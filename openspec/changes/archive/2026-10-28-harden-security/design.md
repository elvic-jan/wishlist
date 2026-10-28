## Context

Password reset (`ForgotPasswordRateLimiter`) and invite codes (`InviteCodeAttemptLimiter`) already use in-memory `PartitionedRateLimiter`s. That relies on the Container App's single replica (hosting spec). The SPA is served by the API (`SpaPage`, static files), so headers set in the API reach every page and asset. `index.html` holds one inline script, the first-paint theme script. The app loads no third-party scripts, styles, fonts or images.

## Goals / Non-Goals

**Goals:** close the open redirect, stop online password guessing, clear the known dependency advisories, and add standard browser protections without breaking the app.

**Non-Goals:** the remaining review findings (see proposal), account lockout stored in the database, CAPTCHAs.

## Decisions

**1. Redirect check by URL resolution, not string prefixes.**
`safeRedirect` rejects any value containing a backslash or starting with `//`, then resolves it against the page's origin with `new URL` and keeps it only when the origin is unchanged. It returns the resolved path, query and hash. A prefix check alone missed `/\host`; resolving catches every other form a browser treats as another host. The react-router update closes the same hole inside `navigate`.

**2. Count failures, check before the password.**
`ILoginAttemptLimiter` has `IsBlocked(email, ip)` and `RecordFailure(email, ip)`, backed by two fixed-window limiters: 10 failures per normalized email, 30 per IP, 15-minute windows. `LoginUserUseCase` checks first and throws `TooManyRequestsException` (429) when blocked, so a correct guess is never confirmed. It records a failure for unknown emails and wrong passwords alike. A successful sign-in doesn't reset the window; at 10 attempts that never bothers a real person. *Trade-off:* someone can block password sign-in for a known email for up to 15 minutes. Passkey sign-in and password reset still work, and the message says so. *Alternative:* ASP.NET Identity lockout in the database. That survives restarts, but it adds writes on every failure and locks passkeys too unless worked around. The in-memory limiter matches the existing ones.

**3. One middleware for headers, CSP hash computed from the served page.**
`SecurityHeadersMiddleware` runs first and sets headers through `Response.OnStarting`, so static files, the SPA page, API JSON and error responses all get them. The CSP is:

`default-src 'self'; script-src 'self' 'sha256-…'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; manifest-src 'self'; worker-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'`

The `sha256` values come from the inline `<script>` blocks of the built `index.html`, computed once at startup. If someone edits the theme script, the hash follows automatically. `style-src` keeps `'unsafe-inline'` because UI libraries (Radix's scroll lock) inject `<style>` tags; inline styles can't run code. HSTS (`max-age=31536000`, no `includeSubDomains` so other subdomains of the user's domain aren't affected) is sent only outside development and only on HTTPS requests. In production the scheme comes from the ingress's forwarded header.

**4. Dependencies via `npm audit fix`, no forced majors.**
All advisories were fixable within the current major versions.

## Risks / Trade-offs

- [A strict CSP blocks something the app needs] → the app was checked in the browser with the production build served by the API; any violation shows in the console. Everything the app loads is same-origin.
- [In-memory limits reset when the app scales to zero] → an attacker who keeps guessing keeps the replica alive, so the counters stay. Same reasoning as the existing limiters.
