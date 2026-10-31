## 1. Name in the app

- [x] 1.1 Replace "GiftCircles" with "Kadoozle" in `index.html` (title, og/twitter tags, apple title), `manifest.webmanifest` (name, short_name), `AppShell`, `AuthLayout`, `InstallApp`, `UpdateToaster`, `inviteMessage`, `webauthn` (wrong-address hint names kadoozle.app), `LoginPage`, `SecurityPage`, `ConfirmEmailPage`
- [x] 1.2 Invite link preview title in `Program.cs`
- [x] 1.3 All email texts (account confirmation, passkey added, password reset and changed, invitation, buddy, reservation reminder and expiry) and the legacy import message
- [x] 1.4 `Smtp:FromName` in `appsettings*.json`, `smtpFromName` in `containerapp.bicep`, `senderDisplayName` in `email.bicep`
- [x] 1.5 CI warning text in `ci.yml`, fallback URL in `deploy.yml`, `infra/README.md` examples
- [x] 1.6 Update tests that assert on the old name

## 2. Link-preview image

- [x] 2.1 New `og/kadoozle.png` (same icon, colours and tagline, name "Kadoozle"); point `index.html` and the invite preview at it; remove `og/giftcircles.png`

## 3. Passkeys and the move message

- [x] 3.1 Migration that deletes all stored passkeys (all were made for giftcircles.app)
- [x] 3.2 Check that the RP ID resolves to `kadoozle.app` from `https://www.kadoozle.app`; update the `PasskeyConfigOptions` comment
- [x] 3.3 Message "GiftCircles is now Kadoozle" for accounts created before the move, with an "Add a passkey" action to the security page
- [x] 3.4 Test: a passkey-only account recovers by email and adds a new passkey

## 4. Infra for a second address

- [x] 4.1 `containerapp.bicep`: optional `previousCustomDomainName` kept bound with its certificate; base URL follows the new domain only once its certificate is ready
- [x] 4.2 `main.bicep` and `infra.yml`: pass `PREVIOUS_CUSTOM_DOMAIN`
- [x] 4.3 Document the switch steps in `infra/README.md`

## 5. Release

- [x] 5.1 Release note in `index.html`, wording confirmed with the user
- [x] 5.2 API tests, SPA lint and build pass; no user-visible "GiftCircles" left except the move message
- [x] 5.3 Local check: served page, manifest and preview image (browser pane rejected the dev certificate; move message covered by tests)

## 6. Switch-over (after the PR is merged; do NOT approve its deploy until 6.5)

The deploy removes all passkeys. Deployed before the address switch, people would add new passkeys for giftcircles.app that break again at the switch, so the infra steps come first.

- [x] 6.1 Merge the PR; leave its **deploy** run waiting for approval
- [x] 6.2 Cloudflare kadoozle.app: `CNAME www` to the app FQDN (DNS only) and `TXT asuid.www`
- [x] 6.3 Email: set `EMAIL_DOMAIN=kadoozle.app`, `EMAIL_DOMAIN_VERIFIED=false`, run infra (mail goes out from the azurecomm.net address meanwhile); add the listed records plus `_dmarc` (`p=quarantine`) in Cloudflare (DNS only); once verified set `EMAIL_DOMAIN_VERIFIED=true`
- [x] 6.4 Set `CUSTOM_DOMAIN=www.kadoozle.app`, `CUSTOM_DOMAIN_CERT_READY=false`, `PREVIOUS_CUSTOM_DOMAIN=www.giftcircles.app`; run infra
- [x] 6.5 Set `CUSTOM_DOMAIN_CERT_READY=true`; run infra (new certificate, base URL switches); set `PUBLIC_SITE_URL=https://www.kadoozle.app`; now approve the waiting deploy (re-run it if it timed out)
- [x] 6.6 Cloudflare: kadoozle.app proxied `A @` and apex-to-www redirect; giftcircles.app apex and www proxied and redirected to `https://www.kadoozle.app` with path and query
- [x] 6.7 Clear `PREVIOUS_CUSTOM_DOMAIN`; run infra
- [x] 6.8 Check in production: new address, old invite link redirect, email from noreply@kadoozle.app passes DMARC, sign-in with a new passkey, move message shown
- [x] 6.9 giftcircles.app and kadoozle.app on auto-renew in Cloudflare
