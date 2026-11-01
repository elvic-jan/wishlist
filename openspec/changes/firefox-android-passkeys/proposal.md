## Why

Passkeys don't work in Firefox on Android (GitHub issue #115). Creating an account with fingerprint succeeds, but signing in with that passkey then fails, and adding the fingerprint again through "Can't sign in?" gives another passkey that fails the same way. Someone who signed up with only a passkey ends up in a loop with no way in on that phone.

The likely cause is how Firefox on Android stores passkeys: our sign-in asks the browser for any passkey for the site without an email (discoverable credentials, `residentKey: required`), and Firefox on Android doesn't find the passkey it just created that way. Firefox on Windows, Mac and iPhone/iPad use the system's passkeys and are not affected. Linux is not a target.

## What Changes

- On Firefox for Android the SPA treats passkeys as unsupported, like a browser without the WebAuthn API: no "Face ID / fingerprint" option for signing in, creating an account, "Can't sign in?" recovery, the add-a-passkey prompt, or adding one on Sign-in & security. Password sign-in, sign-up and recovery work as before.
- On Sign-in & security in Firefox for Android, instead of the add button, the page says fingerprint sign-in doesn't work in Firefox on Android and to use Chrome on this phone, or the password.
- Detection is by user agent (Android + Firefox), because Firefox on Android reports passkey support through the feature checks even though sign-in fails.
- No server changes; passkeys already created stay listed and can be removed.
- Release note: "In Firefox on Android, sign in with your password"

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `user-auth`: new requirement that passkey options are offered only on browsers where they work.

## Impact

- **SPA**: `lib/webauthn.ts` (`passkeysSupported`, Firefox-for-Android check), `SecurityPage.tsx` (message), `index.html` (release note).
- No API, data or contract changes.
