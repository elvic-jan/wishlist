## Context

`add-invite-only-signups` made registration invite-only, with one stop-gap: while the database has zero users, `RegistrationGate.RequireBootstrapAsync` lets a codeless sign-up through, and `GET /api/auth/registration` tells the SPA whether to show the form. That design said the stop-gap would go once there was another way to create the first account.

Building blocks that already exist:

- **Job mode** in `Program.cs`: the same image runs `--job <name>` against the configured database and exits (`reservations`, `legacy-load`). `infra/README.md` already shows how to run a job from a PC against production (temporary firewall rule, admin connection string).
- **Account recovery link**: `RequestPasswordResetUseCase` makes an Identity reset token and builds `<PublicBaseUrl>/recover?email=…&token=…`. On that page the person adds a passkey (which also confirms the email and ends other sessions) or sets a new password. Valid 30 minutes, single use.
- `IUserRepository.CreateWithoutPasswordAsync`, `MarkEmailConfirmedAsync`, `GeneratePasswordResetTokenAsync`.

## Goals / Non-Goals

**Goals:**
- No codeless sign-up path anywhere, on any database.
- A way to create the first account on a fresh install.
- A way for the owner to get back into an account when email or sign-in is broken.

**Non-Goals:**
- An admin UI or admin role.
- Creating the first circle or invites from the command line. Once signed in, the owner does that in the app.
- Changing an account's email, or other repairs. Those stay manual SQL (see infra/README).

## Decisions

### 1. Delete the bootstrap, don't hide it behind a flag

`RegistrationGate` treats a missing code exactly like any refused invite: 403 "Registration is by invitation only." It still runs before the duplicate-email check, so nothing leaks. `CheckAsync` then always returns a `GroupInvitation` (non-null), and the register and passkey sign-up use cases drop their `null` handling. `GetRegistrationStatusUseCase`, `RegistrationStatusDto`, the controller action and their DI registration are deleted.

Alternative: keep the bootstrap behind a config switch (`Registration:AllowBootstrap`). Rejected: it's the codeless path the user wants gone, just harder to see, and a wrong setting would reopen it.

### 2. One job, `owner-access`, for both the first account and a rescue

`--job owner-access --email <email> [--name <name>]`:

1. Look up the account by email.
2. If there is none: `--name` is required (otherwise fail with a clear message). Create it with `CreateWithoutPasswordAsync`, no circle, and mark the email confirmed. The operator typed the address, so it counts as proved.
3. Make a recovery token and print the recovery link and its expiry to the console. Don't send an email. A broken mailbox or mail setup is one reason to run the job.

Logic lives in a new application use case (`OwnerAccessUseCase`), next to the recovery use case. It reuses one shared link builder, which is pulled out of `RequestPasswordResetUseCase`. `Program.cs` only parses the arguments and writes the result.

The recovery page already does what is needed: add a passkey or set a password, then sign in. A fresh passkey-only account works there. Rescue gets the right side effects for free: the token is single use, and recovery ends every other session.

Alternatives:
- Job takes `--password`. Rejected: the password ends up in shell history, and passkey-first is the default.
- Separate `create-account` and `reset-access` jobs. Rejected: they share all but one step, and one command is easier to remember in an emergency.
- Seeding the first account from configuration at startup. Rejected: secrets in config, and it runs on every start.

### 3. The link uses `App:PublicBaseUrl`

The job builds the link from `App:PublicBaseUrl`, like the email does. When the job runs from a PC with the `https` launch profile, that setting points at localhost. So the README command passes `--App:PublicBaseUrl https://www.kadoozle.app`. The job prints the link as built and doesn't guess.

### 4. Tests that need a first user

Tests that relied on a codeless first sign-up get their first user from a test helper that creates a user (and a circle with an invite where they need one) directly, the same way most API tests already set up data. The Postgres end-to-end passkey test starts with an invite created through a seeded owner.

## Risks / Trade-offs

- [The printed link is a sign-in credential for 30 minutes] → Only someone who already has the admin connection string can run the job. The link is single use, short-lived and only printed to that console. README says to close the firewall rule afterwards.
- [A typo in `--email` creates a stray account] → The job prints whether it created an account or found one, and the account has no circle. Delete it with SQL if needed.
- [A fresh install now needs a terminal step] → One documented command, run once.
- [Old SPA builds still call `/api/auth/registration`] → They get 404, and the existing `catch` already treats that as "invite required". No harm.

## Migration Plan

Deploy as usual. Production already has users, so no one notices a change. Rollback: redeploy the previous image.

## Open Questions

None.
