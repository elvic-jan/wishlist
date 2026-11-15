## Why

Families and friend groups using Kadoozle at Christmas want to draw names so each person buys for one secret recipient, and today they do it on paper or in another app, away from the wishlists the gift-giver then needs. Issue #101.

## What Changes

- Any signed-in user can create a **Secret Santa drawing**: a temporary circle with a title, an end date (required) and an optional budget text.
- The organizer adds participants from three sources, merged without duplicates: whole circles (with the option to untick members), individual people they share a circle with, and **name + email** for people who aren't in the app yet or don't share a circle with the organizer. The organizer is ticked as a participant by default and can untick themself.
- The organizer sets **exclusions** as pairs (for example partners). Exclusions work in both directions.
- Every participant is invited by email and gets an in-app card with **Join / Decline**. Drawings they haven't answered show at the top of their circles list, and opening one offers the same choice. People without an account join by registering through a personal link. If a typed email belongs to an existing account, that person gets the same invitation, and the organizer's screen looks the same either way.
- A participant who has joined can **join on behalf of a friend whose wishlist they are a buddy for**, by tapping that friend's name in the participants list.
- The participants list shows each person's status: Joined, Invited or Declined. Until a participant has joined, other participants who don't already share a permanent circle with them see only their name, not their wishlist.
- Until the draw, the organizer can add and remove participants. Removed people are not told.
- The organizer presses **Draw names**. If some participants haven't answered, the organizer sees them listed and must confirm, and they are included. The draw is a single loop (A→B→C→…→A) that respects exclusions and needs at least 3 participants. If no valid loop exists, nothing is drawn and the organizer is told why.
- After the draw, each participant gets an email with the person they drew, the budget and a link to that person's wishlist (or a join link if they haven't joined yet). An in-app "your recipient" card shows the same information. On the Wishlists page, the drawn person appears at the top, below "My wishlist", tagged with the drawing's title. Nobody, including the organizer, can see the other assignments.
- A participant who declines after the draw is not replaced. The organizer gets an email saying who declined and that their recipient now has no Secret Santa.
- The organizer can **cancel** a drawing: participants get an email and no longer see it, and the organizer can **undo** the cancellation until cleanup. Undoing it brings everything back as it was, with an email to participants.
- After the end date, the drawing stays visible in a subdued style for one month. The monthly maintenance job permanently deletes ended drawings and cancelled drawings once a month has passed.
- All new texts and emails are in English and Dutch.

Out of scope (separate issues): joining or leaving after the draw with loop repair (#154), reveal after the end date (#155), avoiding last year's pairs (#156), nudging participants with an empty wishlist (#157).

## Capabilities

### New Capabilities
- `secret-santa`: creating a drawing, adding participants (circles, people, email invitations) without duplicates, exclusions, join/decline (including joining on behalf of a friend through a buddy), participant statuses and visibility, drawing names, the recipient email and card, decline after the draw, cancel and undo, the end date, and cleanup.

### Modified Capabilities
- `groups`: a circle can be a drawing. The circles list puts unanswered drawings at the top and shows ended drawings in a subdued style. Drawings don't offer the invite-link and leave actions that permanent circles have, and a cancelled or expired drawing no longer counts as a shared circle.
- `wishlists-overview`: the people list includes joined co-participants of active drawings. A person you drew appears at the top, below "My wishlist", tagged with the drawing's title, instead of in their alphabetical place.
- `hosting`: the monthly job also deletes drawings that ended or were cancelled more than a month ago.

## Impact

- **Domain/DB**: `Group` gets a kind (circle or drawing) plus drawing fields (end date, budget, organizer, drawn at, cancelled at). New tables for drawing participants (user or name + email, status, personal invite code), exclusions and assignments. One additive migration.
- **Application**: new use cases for creating a drawing, managing participants, answering an invitation (also for a buddy), drawing names, cancelling and undoing, recipient lookup and cleanup. Registration accepts a drawing invite code next to the existing invitation codes. `SharesGroupAsync` and the circle-mates queries ignore cancelled and expired drawings, and count only joined members.
- **API**: new endpoints under `api/drawings`. `groups/me` and `wishlists/people` return the drawing information.
- **Web**: a create-drawing dialog, a drawing page (participants with status, exclusions, Draw names, Cancel/Undo), an invitation card and a recipient card, the circles list and Wishlists page changes, and register/join for drawing invites. English and Dutch texts.
- **Email**: drawing invitation, your recipient, cancelled, back on, and someone declined after the draw (to the organizer), all in the recipient's language.
- **Jobs**: the monthly reservation job also runs the drawing cleanup.
- **Release note**: a new feature note, wording to be agreed with the user.
