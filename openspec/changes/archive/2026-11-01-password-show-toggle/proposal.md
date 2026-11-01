## Why

Chrome (on Mac and elsewhere), Safari and Firefox show no button to reveal a password while typing it; only Edge has one built in. People mistype passwords they can't see, especially on phones. GitHub issue #85.

## What Changes

- Every password field on the Login, Register and Recover pages gets an eye button inside the field that shows or hides the typed password. On Recover, the new and confirm fields each have their own button.
- Edge's built-in reveal button (`::-ms-reveal`) is hidden, so there is only one.
- Release note: "Option to unmask password"; shipping together with the Wishlists-home change, the combined note is "Wishlists is now the homepage. Improved password unmasking."

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `user-auth`: new requirement "Password fields can be shown".

## Impact

- **SPA**: new `PasswordInput` component in `components/ui`; `LoginPage.tsx`, `RegisterPage.tsx`, `RecoverPage.tsx` use it; `index.css` hides `::-ms-reveal`; release note in `index.html`.
- No API or data changes.
