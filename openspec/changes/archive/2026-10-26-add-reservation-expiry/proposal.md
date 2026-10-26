## Why

A reservation lasts forever today: if someone reserves a gift and forgets about it, nobody else can give it. Reservations should run out after a while, but the reserver should first get a chance to keep theirs.

## What Changes

- A new **monthly scheduled job** (on the 1st of each month) sends the emails and ends reservations. It's the app's first scheduled job.
- A reservation (status "Reserved") runs out at the first monthly run that is **at least 6 months** after it was made, so between 6 and 7 months. The item is then free again for anyone in the circle, and **the reserver gets an email** saying their reservation ran out. The owner is never told.
- **One month before that** (so after at least 5 months), the reserver gets a reminder email that the reservation is still open. It links to the wishlist, where they can keep the reservation, mark it as bought, or release it. Because both dates fall on a monthly run, the reminder always comes a full month before the end.
- **Keeping a reservation** starts a new period, with a new reminder a month before its end.
- **Bought never runs out.** Marking a gift as bought ends the timer.
- The reserver sees on the wishlist when their reservation ends ("Your reservation ends on 1 May 2027."). Other givers see only "Reserved", and the owner still sees nothing.
- Existing reservations count from the day they were made.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `gift-claiming`: reservations expire after 6 months (at a monthly run), a reminder email goes out a month before, an email when one has expired, and the reserver can keep (extend) a reservation.
- `hosting`: a monthly scheduled job runs alongside the app.

## Impact

- **Domain / database**: `GiftClaim` gets `ExpiresAt` and `ReminderSentAt`; an EF Core migration fills `ExpiresAt` for existing reservations.
- **Application**: expiry rules when reading and reserving; a "keep reservation" use case; a use case that sends reminders, ends expired reservations, and emails the reservers.
- **API**: new `POST /api/wishlists/{userId}/items/{itemId}/claim/extend`; the claim in a member's wishlist response gets `expiresAt` (only for the claimer). The same image can start in "job mode" instead of as a web server.
- **Web**: the member wishlist page shows the end date and a "Keep reservation" button.
- **Email**: two new emails (reminder, expired), sent through the existing email sender.
- **Infra**: an Azure Container Apps scheduled job (`caj-wishlist-reservations`) in the existing environment, with the app's secrets; the deploy workflow also updates its image. Cost: a few seconds of compute per month, within the free allowance.
