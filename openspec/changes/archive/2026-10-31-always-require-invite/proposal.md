## Why

Sign-up is invite-only, but one codeless path is still in the product: while the database has zero users, anyone can register without a code. It was a stop-gap from `add-invite-only-signups` (GitHub issue #77). We want no codeless path at all, even one that closes itself. That needs another way to create the first account on a fresh install, and the same tool also gives the owner a way back in if their account ever breaks.

## What Changes

- **BREAKING**: Registration (password and passkey) always requires a valid invitation code. A codeless sign-up is refused with 403, even on an empty database.
- **BREAKING**: Remove `GET /api/auth/registration` (the registration-status endpoint) and its use case and DTO.
- The SPA's `/register` page without a code always shows the "invitation only" card. It no longer calls the server to decide.
- New job `--job owner-access --email <email> [--name <name>]`, run with the same image and the database connection string, like `legacy-load`:
  - If no account has that email, it creates one (needs `--name`), without a password and without a circle, and marks the email as confirmed.
  - Either way it prints a single-use sign-in link to the console (the existing "get back into your account" link, valid 30 minutes). The link lets the owner add a passkey or set a password. Nothing is emailed.
- `infra/README.md` documents the job for both cases: the first account on a fresh install, and getting back into a broken account.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `user-auth`: "User registration" and "Passkey registration through an invitation" drop the first-account exception. "Registration status endpoint" is removed. "Sign-up UI is invitation-driven" no longer depends on the status endpoint. New requirement: an operator job that creates an account or issues a sign-in link from the command line.

## Impact

- **API**: `RegistrationGate` (bootstrap branch removed), `AuthController` (status endpoint removed), `GetRegistrationStatusUseCase` and `RegistrationStatusDto` deleted, `Program.cs` gains the `owner-access` job, a new application use case for it.
- **SPA**: `RegisterPage.tsx` (`CodelessRegistration` removed), `AuthContext.tsx` comment on the `null` joined group.
- **Tests**: `FirstAccountBootstrapTests` replaced by tests that a codeless sign-up is refused on an empty system and by tests for the new job. Tests that rely on a codeless first sign-up (`AuthSessionCookieTests`, `PasskeyPostgresEndToEndTests`, others) create their first user another way.
- **Ops**: production already has users, so nothing changes for anyone signing up today. `infra/README.md` gains a section on the job.
