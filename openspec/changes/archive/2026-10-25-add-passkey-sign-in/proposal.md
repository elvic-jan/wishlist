## Why

Relatives forget passwords, reuse weak ones, and type them on shared computers. Passkeys (Face ID, fingerprint or device PIN, synced by iCloud Keychain, Google Password Manager or a password manager) remove the password without adding a third-party identity provider, can't be phished, and leave nothing useful in a database leak. .NET 10 ASP.NET Core Identity ships passkey support, and the app now has a stable domain (`giftcircles.app`) to bind passkeys to.

## What Changes

- **Passkey sign-in**: the sign-in page offers "Sign in with Face ID / fingerprint" and passkey autofill on the email field. A successful passkey sign-in ends exactly like a password sign-in today: an access token plus the rotating refresh cookie.
- **Passkey-first sign-up through an invite**: the invite sign-up form asks for name and email and creates a passkey; "Use a password instead" keeps the password form. Invite gate, guessing limit and the first-account bootstrap behave as today.
- **Manage passkeys**: a "Sign-in & security" page lists passkeys (add, rename, remove), shows whether a password is set and whether the email is confirmed, and can sign out all other devices. The last remaining way to sign in cannot be removed.
- **Existing password users** get a dismissible "Sign in with Face ID next time?" prompt after signing in.
- **Email confirmation (non-blocking)**: every new account gets a confirmation link; the app is usable immediately and shows a reminder until the address is confirmed.
- **Recovery replaces "forgot password"**: "Can't sign in?" emails a 30-minute single-use link that lets the person either add a passkey on the device they're on or set a new password. The email goes only to accounts whose address is confirmed or that have a password (existing accounts). Using the link confirms the address and signs out every other session.
- **Notifications**: adding a passkey sends an email to the account.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `user-auth`: adds passkey sign-in, passkey registration through an invitation, passkey management, email confirmation and passkey recovery; the password-reset request now targets eligible accounts only and its link offers a passkey as well as a new password, and redeeming it confirms the email.

## Impact

- **Database**: Identity schema version 3 (adds `AspNetUserPasskeys`; version 2 also tightens a few Identity column lengths). One migration.
- **Configuration**: `Passkeys:ServerDomain` (defaults to the host of `App:PublicBaseUrl` without a leading `www.`, so production uses `giftcircles.app`); allowed origins default to `App:PublicBaseUrl`. No infra change needed.
- **API**: new anonymous endpoints under `/api/auth/passkey/*`, `/api/auth/recovery/passkey*`, `/api/auth/confirm-email`; new authenticated endpoints `/api/auth/security`, `/api/auth/passkeys*`, `/api/auth/confirm-email/resend`, `/api/auth/sessions/revoke-others`. Existing endpoints keep their contracts.
- **Web**: sign-in, sign-up, forgot-password ("Can't sign in?"), a recovery page (also serving old `/reset-password` links), email-confirmation page, a "Sign-in & security" page, shell banners.
- **Out of scope**: third-party identity providers, passkey-only accounts enforcement, re-authentication before sensitive changes (see design), "last used" timestamps.
