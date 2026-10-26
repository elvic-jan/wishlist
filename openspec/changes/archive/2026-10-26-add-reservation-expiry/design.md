## Context

A reservation is a `GiftClaim` row with `Status = Reserved` and `ClaimedAt`. It disappears only when the claimer releases it; marking it bought sets `Status = Bought`. The app runs on Azure Container Apps with **zero minimum replicas** (at most one), so nothing runs while nobody uses it. There are no scheduled jobs yet: the GitHub workflows run on push to `main` or by hand, and the only automation in Azure is the monthly budget alert. Emails go through `IEmailSender` (Azure Communication Services in production, SMTP locally). The owner must never learn anything about claims on their own items.

## Goals / Non-Goals

**Goals:**
- Reservations end after at least 6 months, unless the reserver keeps them.
- One reminder email to the reserver, a full month before the end (so after at least 5 months), with a way to keep the reservation.
- One email to the reserver when their reservation has ended.
- One scheduled job, run monthly.
- Bought claims never end.

**Non-Goals:**
- Occasion dates (birthdays, events) and tying expiry to them.
- Notifying the owner of anything.
- One-click "keep" links that work without signing in.

## Decisions

### 1. End dates fall on a monthly run
The job runs at **07:00 UTC on the 1st of each month**. A reservation's end date is "6 months from now, rounded up to the next run": `ExpiresAt = NextRunOnOrAfter(now + 6 months)`, where a run is the 1st of a month at 07:00 UTC. The reminder is due at `ExpiresAt - 1 month`, which is also a run.

Example: reserved on 2 January → 2 July + rounding → ends 1 August, reminder on 1 July. Reserved on 31 January → ends 1 August, reminder on 1 July.

So a reservation lasts between 6 and 7 months, the reminder comes after 5 to 6 months, and the reserver always gets a full month's notice. The end date shown in the app is exactly when the job frees the item and sends the "ran out" email.

*Why round?* With a monthly job and exact dates, the reminder could arrive one day before the end (reserved on 2 January: reminder window 2 June to 2 July, and the only run in it is 1 July). The "ran out" email could also arrive up to a month after the item was freed. Rounding to the run fixes both.

The run schedule, the 6-month lifetime and the 1-month lead are named constants in the domain (`ReservationSchedule`), next to the cron expression in Bicep, so they're changed together.

### 2. Store an end date on the claim
Add `ExpiresAt` (nullable) and `ReminderSentAt` (nullable) to `GiftClaim`. `ExpiresAt` is set when reserving or keeping, and becomes `null` when marked bought.

### 3. Reads also respect the end date
A claim with `Status = Reserved` and `ExpiresAt <= now` is treated as **no claim** on read and when reserving, even if the job hasn't run yet (for example if it failed). The job is what sends the email and deletes the row; reads don't depend on it. Domain method: `GiftClaim.IsActive(now)`.

### 4. The monthly job
An **Azure Container Apps job** (`Microsoft.App/jobs`, trigger type `Schedule`, cron `0 7 1 * *`) in the existing environment runs the **same image** as the app with the argument `--job reservations`. `Program.cs` sees the argument, builds the services without starting the web server, runs `ProcessReservationsUseCase`, and exits. The job gets the same secrets and settings as the app (database, email, public URL), defined once in Bicep and shared by both. Retry limit 1, timeout 10 minutes.

`ProcessReservationsUseCase`, per run:
1. **Reminders:** Reserved claims with `ReminderSentAt == null` and `ExpiresAt - 1 month <= now < ExpiresAt`. Email the claimer and set `ReminderSentAt`.
2. **Expiry:** Reserved claims with `ExpiresAt <= now`. Email the claimer that it ran out, then delete the claim.

Emails are best-effort per claim: a failed reminder is retried next month (still in time, since it's then the expiry run, so it sends the expiry email instead). A failed "ran out" email is logged and the claim is deleted anyway, so the item never stays blocked. Each run logs its counts.

*Why a Container Apps job and not a timer in the app?* The app sleeps when idle, so an in-app timer only runs when someone visits. The job runs on time, costs a few seconds of compute per month, and needs no public endpoint. *Why not a GitHub Actions cron?* It would need a secret endpoint on the app to call, and GitHub pauses scheduled workflows in repositories without activity for 60 days.

**Deploys:** the deploy workflow updates the job's image right after the app's (`az containerapp job update --image`), so both always run the same code and the job never runs against a schema it doesn't know.

### 5. Keeping a reservation requires being signed in
`POST /api/wishlists/{userId}/items/{itemId}/claim/extend`: claimer only, Reserved only, not yet expired. It sets a new `ExpiresAt` (rule 1, from now) and `ReminderSentAt = null`. The email links to `/wishlist/{ownerId}`, where the item shows "Keep reservation". No tokens in links, so a forwarded email can't act for someone.

### 6. Privacy
`expiresAt` is added to `GiftClaimDto` only when `IsClaimedByMe`. Other givers see "Reserved" as today, and the owner's responses still contain no claim data. Both emails go to the claimer only. They name the item and whose wishlist it's on, nothing about other givers.

### 7. Existing reservations
The migration sets `ExpiresAt` with rule 1 from `ClaimedAt` for existing Reserved claims, and leaves it `null` for Bought ones. Production has only been running since October 2026, so none of them is close to its reminder.

## Risks / Trade-offs

- **A reservation can last up to 7 months**, because of the rounding. That's the price of a monthly job with a guaranteed month of notice.
- **A failed run** delays emails by a month. Reads still free expired items on time, and failures show in the job's execution history and logs.
- **Duplicate email if the job stops between sending and saving.** Rare and harmless.
- **Two places hold the schedule** (cron in Bicep, constants in code). Comments in both point at each other.

## Migration Plan

1. Merge: the deploy workflow runs the migration, deploys the app, then updates the job's image. Until step 2 the job doesn't exist yet, so the workflow skips that step when the job isn't found.
2. Run the **infra** workflow once to create the job (it's in Bicep).
3. Optional: start the job by hand once (`az containerapp job start`) and check its log.

Rollback: re-deploy the previous image to the app and the job. The two new columns are harmless to older code.
