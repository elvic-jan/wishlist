## Context

The app runs as one Azure Container App behind Cloudflare DNS at `www.giftcircles.app` (apex redirects to www via a Cloudflare rule). Email goes out through Azure Communication Services from `noreply@giftcircles.app`. Passkeys use the relying-party ID derived from `App:PublicBaseUrl` without `www.`, so today `giftcircles.app`. `kadoozle.app` is registered at Cloudflare in the same account, with Cloudflare nameservers and no records yet.

The previous rename (`archive/2026-10-25-rename-to-giftcircles`) only changed texts. This one also moves the web address, which touches certificates, email and passkeys.

## Decisions

### 1. Passkeys: clean break to `kadoozle.app` (decided 2026-10-31)

A passkey only works for the domain it was made for. Options:

| | What | For | Against |
|---|---|---|---|
| **A (chosen)** | RP ID becomes `kadoozle.app`; old passkeys deleted at the move | Simple, no lasting tie to the old name; password managers show "kadoozle.app" | Everyone with a passkey adds a new one; passkey-only accounts recover by email |
| B | Keep RP ID `giftcircles.app` and use Related Origin Requests (`/.well-known/webauthn` on giftcircles.app lists www.kadoozle.app) | Existing passkeys keep working | Not supported in Firefox; old name stays in every password manager forever; giftcircles.app must keep serving that file, not just redirect |

Only a few users have passkeys so far, everyone is signed out by the move anyway, and the email recovery path already exists, so A is worth the one-off effort. Old passkeys are deleted by a migration so the security page doesn't list dead entries and the existing "add a passkey" prompt shows again.

### 2. Address switch without downtime

`containerapp.bicep` binds one custom domain today, and `publicBaseUrl` falls back to the `azurecontainerapps.io` address while a certificate isn't ready, which would break passkeys and email links mid-switch. Add an optional `previousCustomDomainName` that stays bound (with its existing certificate) while the new one is set up:

1. Cloudflare (kadoozle.app): `CNAME www` to the container app FQDN (**DNS only**) and `TXT asuid.www`.
2. GitHub vars: `CUSTOM_DOMAIN=www.kadoozle.app`, `CUSTOM_DOMAIN_CERT_READY=false`, `PREVIOUS_CUSTOM_DOMAIN=www.giftcircles.app`, then run **infra** (both hostnames bound, base URL still the old one).
3. `CUSTOM_DOMAIN_CERT_READY=true`, run **infra**: certificate issued, base URL becomes `https://www.kadoozle.app`.
4. Cloudflare (kadoozle.app): proxied `A @` plus a redirect rule apex to www, like giftcircles.app.
5. Cloudflare (giftcircles.app): `www` becomes proxied, and redirect rules send both apex and www to `https://www.kadoozle.app` with path and query. Then clear `PREVIOUS_CUSTOM_DOMAIN` and run **infra** to unbind it.

Step 3 is the moment users are signed out and passkeys switch. The code PR is deployed right after it, not before: its migration deletes all passkeys, and passkeys added on giftcircles.app in between would break again. The deploy workflow waits for approval, so the PR can be merged first and the deploy approved after step 3. In the few minutes between, the old texts run on the new address.

### 3. Email domain

ACS can link more than one verified domain. Add `kadoozle.app` as a second customer-managed domain (verification TXT, SPF in the same `TXT @`, two DKIM CNAMEs DNS only, `_dmarc` with `p=quarantine`), wait for verification, then switch `EMAIL_DOMAIN` and the sender. Keep the giftcircles.app domain and its DNS records for a while so DMARC reports stay valid.

### 4. Internal names stay

Browser storage keys (`giftcircles.*`), the refresh lock name, the Data Protection purpose `GiftCircles.PasskeyCeremony.v1`, `GIFTCIRCLES_TEST_DB` and the `Wishlist.*` projects are not visible to users. Renaming them would reset dismissed prompts or invalidate running ceremonies for no gain. Code comments that name the product are updated where touched.

### 5. Icon stays

The current gift icon, favicon and app icons stay; an own logo is a separate change. The link-preview image `og/giftcircles.png` has the name in it, so it is redrawn as `og/kadoozle.png` with the same icon, colours and tagline.

## Risks

- **Users locked out**: passkey-only accounts depend on email recovery. Mitigation: check that emails arrive from the new domain before step 3; the move message tells people what to do.
- **Wrong Cloudflare proxy setting**: the www CNAME must stay DNS only, or the Azure certificate won't renew (lesson from giftcircles.app).
- **Old domain lapsing**: giftcircles.app must keep renewing for years, or old links and the redirect break.

## Open questions

- Release-note wording, e.g. "GiftCircles is now Kadoozle!" (to confirm before the PR).
