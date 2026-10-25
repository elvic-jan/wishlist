## 1. Domain, Application ports and use cases

- [x] 1.1 `User` gains `EmailConfirmed` and `HasPassword`; `IUserRepository` gains passwordless create (with a given ID), email-confirmation token generate/confirm, mark-confirmed, reset-token verification without consuming, and security-stamp rotation
- [x] 1.2 `IPasskeyService` port with plain records: begin/complete sign-in, begin/complete registration (returns a verified passkey + user entity), add, list, rename, remove, count
- [x] 1.3 `ITokenService.RevokeOtherUserRefreshTokensAsync(userId, keepToken)`
- [x] 1.4 Extract the shared registration gate (input, invite/bootstrap, duplicate email) from `RegisterUserUseCase`
- [x] 1.5 Use cases: passkey sign-in, passkey sign-up (begin/complete, one transaction), security overview, add/rename/remove passkey (last-method rule), revoke other sessions, send/confirm email confirmation, passkey recovery (begin/complete)
- [x] 1.6 Emails: confirmation, passkey added, recovery (replaces reset wording; links `/recover`); recovery eligibility rule; reset success confirms email; password sign-up sends confirmation

## 2. Infrastructure

- [x] 2.1 Identity schema version 3 at runtime and in the design-time factory; `AddPasskeys` migration
- [x] 2.2 `PasskeyHandler<ApplicationUser>` registration and `IdentityPasskeyOptions` (RP ID from config/`PublicBaseUrl`, required discoverable credentials and user verification, origin allow-list)
- [x] 2.3 `PasskeyService` over the handler and `UserManager`; map `PasskeyException` to a generic `DomainException` and log the detail
- [x] 2.4 Email-confirmation token provider with a 3-day lifespan
- [x] 2.5 `UserRepository` and `TokenService` additions

## 3. API

- [x] 3.1 Protected ceremony cookie helper (Data Protection, 5 min, kind-bound, cleared on completion); shared refresh-cookie helper
- [x] 3.2 Anonymous endpoints: passkey sign-in options/complete, passkey sign-up options/complete (through the invite-code guard), recovery options/complete, confirm email
- [x] 3.3 Authenticated endpoints: security overview, passkey options/add/rename/remove, resend confirmation, revoke other sessions

## 4. Tests

- [x] 4.1 In-memory host: fake passkey service and capturing email sender
- [x] 4.2 Passkey sign-in, ceremony cookie attributes, missing/expired/wrong-kind/replayed ceremony
- [x] 4.3 Passkey sign-up: success, invite gate, changed details, email taken in between
- [x] 4.4 Management: list, add (+ email), rename, remove, last-method 409, other account's passkey 404, revoke others
- [x] 4.5 Recovery eligibility, recovery passkey flow (token single use, sessions revoked, email confirmed), reset confirms email
- [x] 4.6 Email confirmation: confirm, invalid token, resend, sent on both sign-up paths
- [x] 4.7 Postgres-backed end-to-end test with a software authenticator against the real `PasskeyHandler` (opt-in via `GIFTCIRCLES_TEST_DB`)

## 5. Web app

- [x] 5.1 WebAuthn helper (JSON ⇄ binary, feature detection, cancel detection) and passkey API calls
- [x] 5.2 Sign-in page: passkey button + autofill; "Can't sign in?"
- [x] 5.3 Sign-up page: passkey first, "Use a password instead"
- [x] 5.4 Forgot-password page reworded; recovery page (`/recover`, also `/reset-password`) with passkey and password paths
- [x] 5.5 Email-confirmation page
- [x] 5.6 "Sign-in & security" page and account-menu link
- [x] 5.7 Shell banners: confirm email, one-time "Sign in with Face ID next time?" for accounts without passkeys

## 6. Verification

- [x] 6.1 API tests, SPA lint and build pass; EF reports no pending model changes after the migration
- [x] 6.2 Postgres end-to-end test passes locally
- [x] 6.3 Browser check with a JavaScript software authenticator: sign up with a passkey through an invite, sign out, sign in with the passkey, add/rename/remove on the security page
