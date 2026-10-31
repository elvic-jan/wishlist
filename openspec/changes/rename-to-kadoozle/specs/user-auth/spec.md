## ADDED Requirements

### Requirement: Passkeys belong to kadoozle.app
Passkeys SHALL be created for and checked against the relying party `kadoozle.app`, so they work on `www.kadoozle.app` and `kadoozle.app`. Passkeys made for `giftcircles.app` before the move can no longer sign in; they SHALL be removed at the move, so they no longer show on the security page or count as a way to sign in. Accounts that had only passkeys SHALL get back in through the existing "Can't sign in?" email link and add a new passkey there.

#### Scenario: New passkey works on the new address
- **WHEN** a user adds a passkey on `https://www.kadoozle.app` and later signs in with it
- **THEN** sign-in succeeds

#### Scenario: Old passkeys are cleared
- **WHEN** the move has been deployed
- **THEN** no user has a stored passkey made for `giftcircles.app`, and users without a password and without a passkey can still recover access by email

#### Scenario: Prompted to add a passkey again
- **WHEN** a user whose passkeys were cleared signs in with their password
- **THEN** the app offers to add a passkey, as it does for any account without one
