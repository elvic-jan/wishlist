## 1. Sign-in redirect

- [x] 1.1 `safeRedirect`: reject backslashes and `//`, resolve against the origin, keep only same-origin paths
- [x] 1.2 Update react-router-dom to 7.18.5

## 2. Password sign-in limit

- [x] 2.1 `ILoginAttemptLimiter` and `LoginAttemptLimiter` (10 per email, 30 per IP, 15 minutes, failures only); register in DI
- [x] 2.2 `LoginUserUseCase`: check before the password (429), record failures for unknown emails and wrong passwords; pass the client IP from `AuthController`
- [x] 2.3 SPA: show the 429 message on the login page
- [x] 2.4 Tests: per-email block (also with the right password), per-IP block, success after a few mistakes, endpoint returns 429

## 3. Dependencies

- [x] 3.1 `npm audit fix`; raise `package.json` minimums; `npm audit` reports 0

## 4. Security headers

- [x] 4.1 `SecurityHeadersMiddleware`: CSP with inline-script hashes from `index.html`, frame, sniff, referrer and permissions headers; HSTS outside development on HTTPS
- [x] 4.2 Tests: headers on API and error responses, CSP hash matches the page's inline script, HSTS only when deployed and HTTPS
- [x] 4.3 Check the production build in the browser: no CSP violations, theme script runs, sign-in, wishlists and dialogs work
