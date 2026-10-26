## 1. Domain and database

- [x] 1.1 `ReservationSchedule`: run time (1st of the month, 07:00 UTC), 6-month lifetime, 1-month reminder lead, `EndDateFrom(start)` (rounded up to the next run)
- [x] 1.2 `GiftClaim`: add `ExpiresAt` (nullable) and `ReminderSentAt` (nullable); methods `IsActive(now)`, `Keep(userId, now)`, and `MarkAsBought` clearing `ExpiresAt`
- [x] 1.3 EF Core migration: add both columns; fill `ExpiresAt` from `ClaimedAt` for existing Reserved claims
- [x] 1.4 Reserving sets `ExpiresAt`

## 2. Application

- [x] 2.1 Treat expired reservations as no claim when reading wishlists and when reserving (replace the expired claim instead of 409)
- [x] 2.2 `KeepReservationUseCase`: claimer only, Reserved and not expired; new `ExpiresAt`, `ReminderSentAt` cleared
- [x] 2.3 `GiftClaimDto` gets `ExpiresAt`, filled only for the claimer
- [x] 2.4 `ProcessReservationsUseCase`: send due reminders (set `ReminderSentAt` on success); end expired reservations (send the expiry email best-effort, then delete); log counts
- [x] 2.5 Reminder and expiry email texts (plain text + HTML, like the account emails), linking to `/wishlist/{ownerId}`

## 3. API and job mode

- [x] 3.1 `POST /api/wishlists/{userId}/items/{itemId}/claim/extend`
- [x] 3.2 `Program.cs`: with `--job reservations`, build the services, run `ProcessReservationsUseCase`, and exit without starting the web server

## 4. Infra and deploy

- [x] 4.1 Bicep: share the app's secrets and environment settings; add the scheduled job `caj-wishlist-reservations` (cron `0 7 1 * *`, same image, args `--job reservations`, retry 1, timeout 10 min)
- [x] 4.2 Deploy workflow: update the job's image after the app's; skip when the job doesn't exist yet
- [x] 4.3 `infra/README.md`: the job, how to start it by hand, where its logs are

## 5. Web

- [x] 5.1 Member wishlist page: "Reserved by you · until <date>"; "Keep reservation" button when one month or less is left

## 6. Tests and verification

- [x] 6.1 Tests: end-date rounding (2 Jan → 1 Aug, 31 Jan → 1 Aug, 1 Jan 07:00 → 1 Jul), expiry on read, reserve over an expired claim, keep (claimer only, not expired or bought), bought clears expiry, reminder sent once and not for bought or released claims, expiry email + delete (also when sending fails), owner responses contain no expiry
- [x] 6.2 Build, tests, SPA lint and build, `az bicep build`
- [x] 6.3 Local check: run the API with `--job reservations` against reservations dated back; reminder and expiry emails arrive (local SMTP); "Keep reservation" shows and works
- [x] 6.4 After deploy: infra workflow created the job (verified 2026-10-26: schedule `0 7 1 * *`, args `--job reservations`, same image as the app). Not started by hand; first scheduled run 1 Nov 2026.
