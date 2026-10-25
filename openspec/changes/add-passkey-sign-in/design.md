## Context

Sign-in today is email + password through ASP.NET Core Identity (`UserManager`, no Identity cookies). A successful sign-in returns a 15-minute JWT and sets a rotating 30-day refresh-token cookie (`HttpOnly`, `Secure`, `SameSite=Strict`). Sign-up is invite-only. The SPA and API share one origin in production (`https://www.giftcircles.app`); in development Vite (`https://localhost:5173`) proxies `/api` to the API.

.NET 10 Identity adds passkeys at two levels:
- `SignInManager.MakePasskey*/PerformPasskey*/PasskeySignInAsync`, which keep ceremony state in Identity's cookie schemes and sign in with the Identity cookie.
- `IPasskeyHandler<TUser>` (`PasskeyHandler<TUser>`), which returns the WebAuthn options JSON plus an opaque **state** string, and verifies a browser response against that state. It checks challenge, origin, RP ID hash, user presence and verification, signature, sign counter, algorithm and duplicate credential IDs.
- `UserManager.AddOrUpdatePasskeyAsync/GetPasskeysAsync/RemovePasskeyAsync/FindByPasskeyIdAsync` over the EF store (schema version 3, table `AspNetUserPasskeys`).

## Goals / Non-Goals

**Goals:** passkey sign-in, passkey-first invite sign-up, passkey management, recovery by email, and non-blocking email confirmation, all ending in the existing JWT + refresh-cookie session.

**Non-Goals:** external identity providers; removing passwords; re-authentication prompts before sensitive account changes; per-passkey "last used" tracking.

## Decisions

### D1. Use `IPasskeyHandler<TUser>`, not the `SignInManager` passkey flow
The app has no Identity cookie scheme and issues its own tokens. The handler gives exactly the verification step. It is wrapped in an Application port (`IPasskeyService`) implemented in Infrastructure, so use cases deal in strings and plain records, not Identity types or `HttpContext`.

### D2. Ceremony state lives in a protected, short-lived, HttpOnly cookie
The handler's state carries the challenge (and, for registration, the user entity). Handing it to client script would let a captured assertion be replayed with its state. Instead the API wraps `{kind, state}` with ASP.NET Data Protection (purpose `GiftCircles.PasskeyCeremony.v1`, 5-minute time limit) and stores it in a `passkeyCeremony` cookie (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`). Completing a ceremony always clears the cookie. Each state also carries a one-time ID that the server remembers once used, so a copied cookie can't be replayed together with a captured browser response. This matters because synced passkeys usually report a signature counter of 0, so the counter check can't catch replays. The used-ID list is in memory, consistent with the single-replica deployment the rate limiters already rely on. The `kind` (`signin`, `signup`, `add`, `recovery`) stops a state from one ceremony being used in another. Data-protection keys are already in the database, so state survives a scale-to-zero restart.

### D3. Relying-party ID and origins
- `Passkeys:ServerDomain` sets the RP ID. If unset, it is the host of `App:PublicBaseUrl` without a leading `www.`. So production uses `giftcircles.app` without any infra change, and the app can later move between `www.` and the apex without breaking passkeys. Development uses `localhost`.
- Origin validation is overridden. `clientDataJSON.origin` must equal the origin of `App:PublicBaseUrl`, or one listed in `Passkeys:AdditionalOrigins`, and cross-origin (iframe) use is rejected. This keeps working behind the Vite proxy in development.
- The `azurecontainerapps.io` host can't create or use these passkeys (the browser refuses a mismatched RP ID). The UI shows a plain error there.

### D4. Authenticator options
Discoverable credentials **required** (sign-in without typing an email, and autofill), user verification **required**, attestation `none`, all supported algorithms. Timeout stays at the 5-minute default.

### D5. Passkey sign-up is two calls, and the second re-checks everything
1. `register/options {email, displayName, inviteCode}` runs input validation, the invite gate (or bootstrap) and the duplicate-email check. These are the same checks, in the same order, as password sign-up. It then generates a new user ID and returns creation options for `{id, name: email, displayName}`.
2. `register {email, displayName, inviteCode, credential}` runs the same checks again (an invite can expire or the email can be taken in between) and verifies the attestation. The verified user entity must match the submitted email and display name. Then, in one transaction, it creates the user without a password using that ID, stores the passkey, adds the circle membership and marks the invite used. It then issues the session and sends the confirmation email.
Both calls go through the invite-code guard (guessing limit), like password sign-up.

### D6. Never remove the last way to sign in
Removing a passkey is refused (409) when the account has no password and that is its only passkey.

### D7. Recovery reuses the password-reset token
- **The email:** "Can't sign in?" (the existing `forgot-password` endpoint, same rate limits and enumeration safety) sends a link to `/recover?email=…&token=…`. The token is Identity's password-reset token: 30 minutes, bound to the security stamp.
- **The page** offers two paths:
  - **Create a passkey on this device:** `recovery/passkey/options` and `recovery/passkey` verify the token. The options call verifies it without consuming it. The final call adds the passkey, then rotates the security stamp, which invalidates the token. It also marks the email confirmed, revokes every refresh token, and issues a fresh session.
  - **Set a new password:** the existing reset endpoint, which now also marks the email confirmed.
- **Old links:** `/reset-password` links already in inboxes open the same page.

### D8. Who gets a recovery email
Only accounts whose email is confirmed **or** that have a password. Every account created before this change has a password, so nobody loses the reset they have today. A passkey-only account with an unconfirmed address gets nothing: the reply is the same 202, so this stays enumeration-safe. This closes the "typo in the email at sign-up hands the account to a stranger" hole.

### D9. Email confirmation is non-blocking and longer-lived
- **When it's sent:** every new account (passkey or password sign-up) gets a confirmation email. It can be re-sent from the app.
- **Token lifetime:** confirmation uses Identity's email-confirmation token through a separate data-protection provider with a **3-day** lifespan. Reset tokens keep 30 minutes.
- **Confirming:** the link opens `/confirm-email?userId=…&token=…`, which posts to the API. Nothing in the app waits on confirmation, except the recovery rule in D8.

### D10. No re-authentication before adding a passkey (accepted risk)
Adding a passkey needs a valid session only. Mitigations:
- the 15-minute access token and rotating refresh token;
- an email notification on every added passkey;
- "Sign out all other devices" and passkey removal on the security page;
- recovery also signs out every session.

Re-authentication would need an `auth_time` carried through refresh-token rotation. Left for later.

### D11. Errors are generic
Any verification failure returns 400 with a generic message ("Passkey sign-in failed." / "Passkey couldn't be saved."). The specific `PasskeyException` is logged at warning level. A user cancelling the browser dialog is handled client-side and never reaches the API.

### D12. Front end
- **No WebAuthn library.** A small helper converts the options JSON (base64url fields) to `ArrayBuffer`s and serialises the credential back to JSON. It uses `PublicKeyCredential.parse*OptionsFromJSON` and `toJSON` when the browser has them, and its own conversion otherwise.
- **Sign-in page:**
  - The email field has `autocomplete="username webauthn"` and starts a conditional (autofill) request on load.
  - A "Sign in with Face ID / fingerprint" button runs a modal request, aborting the conditional one first.
- **Older browsers:** without `PublicKeyCredential`, the passkey buttons are hidden and the password forms are shown.
- **Wording:** the UI says "Face ID / fingerprint", with "passkey" used only on the security page.

### D13. Testing
- **API tests** (in-memory host): a fake `IPasskeyService`. They cover the ceremony cookie, the invite gate, the last-method rule, recovery eligibility and token use, email confirmation, and the notification emails.
- **A database-backed end-to-end test:** a software authenticator (ECDSA P-256, `none` attestation) runs the real `PasskeyHandler` against Postgres: sign-up, sign-in, add, sign count and wrong origin. It runs only when `GIFTCIRCLES_TEST_DB` is set, so CI without a database skips it.
- **Browser check:** the built SPA runs with a JavaScript software authenticator in place of `navigator.credentials`.

## Risks / Trade-offs

- **Email is the root of trust for recovery.** This is the same as password reset today. D8 narrows it for new accounts.
- **Ceremony cookie path.** The cookie is scoped to `/api/auth`, so starting a second ceremony in another tab replaces the first. The first then fails with a generic error and the user retries. Acceptable.
- **Schema version 2 column changes.** These shorten `AspNetUserLogins`/`AspNetUserTokens` key columns to 128 characters. Both tables are unused today (no external logins, data-protection tokens), so the migration is safe.

## Migration Plan

1. The deploy applies the `AddPasskeys` migration through the existing migrations bundle before the new image starts.
2. **Rollback:** redeploy the previous image. The extra table is ignored by the old code.
