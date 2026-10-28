## Why

A security review of `main` (2026-10-28) found four issues worth fixing first:

1. **Open redirect after sign-in.** The login page sends people to its `redirect` parameter after they sign in. The check lets `/\evil.example` through, and browsers read that as `https://evil.example`. A crafted GiftCircles link could therefore drop people on a fake "sign in again" page.
2. **Unlimited password guessing.** Password sign-in has no attempt limit or lockout, and passwords need only 8 characters. Password reset and invite codes are already limited; sign-in isn't.
3. **Known vulnerabilities in web dependencies.** `npm audit` reports 15 advisories, among them react-router (including the backslash redirect above) and axios, which ship to browsers.
4. **No security headers.** Pages can be framed by other sites (clickjacking on buttons like Reserve or Delete), and there is no Content-Security-Policy or other standard protective header.

## What Changes

- The login page only follows a `redirect` that stays on GiftCircles: a same-origin path, with no backslashes and no protocol-relative `//`. Anything else goes to My circles.
- **Password sign-in is limited:** after 10 wrong passwords for one email address, or 30 from one IP address, within 15 minutes, further password sign-ins for that address are refused with 429 until the window ends, even with the right password. The message points to Face ID / fingerprint sign-in and password reset, which keep working.
- Web dependencies are updated so `npm audit` reports no vulnerabilities (react-router 7.18.5, axios 1.21.0, vite 8.3.5, postcss 8.5.32), with `package.json` minimums raised to match.
- **Every response carries security headers:** Content-Security-Policy (same-origin scripts plus the hash of the first-paint theme script, no framing, no plugins), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, a restrictive `Permissions-Policy`, and outside development `Strict-Transport-Security`.
- Out of scope (later batch from the same review): hashing stored refresh tokens, a least-privilege database login, input length limits, refusing the placeholder JWT key, building invite links from the configured address.

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `user-auth`: password sign-in is rate-limited; the post-sign-in redirect stays on the site.
- `hosting`: security headers on every response.

## Impact

- **Application**: `ILoginAttemptLimiter`; `LoginUserUseCase` checks it before the password and records failures.
- **Infrastructure**: `LoginAttemptLimiter` (in memory, like the other limiters; fine with one replica).
- **API**: `SecurityHeadersMiddleware`; the login endpoint passes the client IP.
- **Web**: `safeRedirect` on the login page; updated `package.json` and lockfile.
- **Tests**: login limit, redirect rules, headers and CSP hash.
