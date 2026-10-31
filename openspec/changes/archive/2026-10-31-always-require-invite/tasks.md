## 1. Remove the codeless sign-up

- [x] 1.1 `RegistrationGate`: a missing invite code throws the same 403 "Registration is by invitation only." (still before the duplicate-email check); delete `RequireBootstrapAsync`; `CheckAsync` returns a non-null `GroupInvitation`
- [x] 1.2 `RegisterUserUseCase` and `PasskeySignUpUseCase`: drop the `null` invitation branch (always join the circle, always return its ID)
- [x] 1.3 Delete `GetRegistrationStatusUseCase`, `RegistrationStatusDto`, the `GET /api/auth/registration` action and the DI registration
- [x] 1.4 Remove the now-unused `IUserRepository.AnyAsync`

## 2. Owner access job

- [x] 2.1 Pull the recovery link builder out of `RequestPasswordResetUseCase` into a shared helper both use
- [x] 2.2 `OwnerAccessUseCase(email, name?)`: find the account or create it (`CreateWithoutPasswordAsync`, confirm email; name required, input limits apply), make a recovery token, return created/found, the link and its expiry; register it in DI
- [x] 2.3 `Program.cs`: `--job owner-access --email <email> [--name <name>]`, clear errors for a missing `--email` or `--name`, print the result; add it to the job list comment and the "unknown job" message

## 3. SPA

- [x] 3.1 `RegisterPage.tsx`: `/register` without a code renders `InvitationOnly` directly; delete `CodelessRegistration` and the `/auth/registration` call
- [x] 3.2 `AuthContext.tsx`: `register` always resolves to the joined circle; update the comment and type

## 4. Tests

- [x] 4.1 Replace `FirstAccountBootstrapTests` with: codeless password and passkey sign-up on an empty system get 403 and create nothing; `/api/auth/registration` is gone (404)
- [x] 4.2 Move tests that relied on a codeless first sign-up (`AuthSessionCookieTests`, `SecurityHardeningTests`, `PasskeyTests`, `InMemoryApi` helpers, `PasskeyPostgresEndToEndTests`) to a seeded owner with an invite
- [x] 4.3 Tests for `OwnerAccessUseCase`: creates a confirmed, passwordless account without a circle; finds an existing one unchanged; refuses a new email without a name; the returned link works on the recovery endpoints (passkey and new password) and only once; no email is sent

## 5. Docs and release

- [x] 5.1 `infra/README.md`: "Owner access" section (firewall rule, admin connection string, `--App:PublicBaseUrl https://www.kadoozle.app`, run the job, open the link, delete the rule) for a fresh install and for getting back in; drop the "register right after first deploy" bootstrap advice
- [x] 5.2 Release note in `index.html`: "Cleanup of some old code" (confirmed with the user; ships with a later release, not deployed on its own)
- [x] 5.3 API tests, SPA lint and build pass
- [x] 5.4 Local check: the job against the local database (missing name refused, create, find); browser pane rejected the dev certificate, so the page and link flow are covered by tests (Postgres end-to-end included)
