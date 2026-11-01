## ADDED Requirements

### Requirement: Passkey options only where they work
The SPA SHALL offer passkey ("Face ID / fingerprint") options for signing in, creating an account, recovering access and adding a passkey only on browsers that have the WebAuthn API and are not Firefox for Android. On other browsers the SPA SHALL offer only the password options, and the Sign-in & security page SHALL explain why no passkey can be added. Passkeys already on the account SHALL still be listed and removable there.

#### Scenario: Firefox for Android gets password options only
- **WHEN** a visitor opens Sign in, Create account or a "Can't sign in?" link in Firefox for Android
- **THEN** no "Face ID / fingerprint" option is shown and the password options work as usual

#### Scenario: Explanation on the security page
- **WHEN** a signed-in user opens Sign-in & security in Firefox for Android
- **THEN** there is no button to add a passkey, and the page says fingerprint sign-in doesn't work in Firefox on Android and suggests Chrome on that phone or the password

#### Scenario: No add-a-passkey prompt
- **WHEN** a signed-in user without passkeys uses Kadoozle in Firefox for Android
- **THEN** the banner suggesting to add Face ID / fingerprint is not shown

#### Scenario: Other browsers unchanged
- **WHEN** a visitor uses Chrome, Edge, Samsung Internet, Safari, or Firefox on Windows, Mac, iPhone or iPad
- **THEN** the passkey options are offered as before
