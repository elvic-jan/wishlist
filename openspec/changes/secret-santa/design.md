## Context

Circles (`Group` + `GroupMember`) are the only thing that connects people in Kadoozle: you can see someone's wishlist, reserve from it, or make them a buddy only if `SharesGroupAsync` finds a circle you're both in. Existing users are invited into a circle through `CircleInvitation` (email + Join/Decline card on My circles). New people sign up only with an invite code (`GroupInvitation`, 3 days, multi-use), which `RegistrationGate` checks. A monthly Container Apps job (`--job reservations`, 1st of the month at 07:00 UTC) runs `ProcessReservationsUseCase`. All texts are in English and Dutch, and emails go out in the recipient's language, falling back to the language of the person who caused them.

A Secret Santa drawing (#101) is a temporary circle with a lifecycle, its own participant list (which may include people without an account), exclusions and secret assignments.

## Goals / Non-Goals

**Goals:**
- Reuse circle membership for wishlist access, so the "names only until joined" rule needs no new access check.
- Assignments stay secret: each person can only ever read their own recipient, and the organizer reads none.
- Never reveal through the organizer's screens whether an email address has an account.
- Drawings disappear by themselves: hidden one month after the end date or cancellation, deleted by the monthly job.

**Non-Goals:**
- Changing the participant list after the draw, or repairing the loop (#154).
- A reveal (#155), history and no-repeat (#156), or nudges for empty wishlists (#157).
- Several organizers, or handing the organizer role to someone else.

## Decisions

### 1. A drawing is a `Group` with a 1:1 `Drawing` row
`Group` gets `Kind` (`Circle` | `Drawing`). Drawing data lives in a `Drawings` table keyed by `GroupId`: `OrganizerId`, `EndDate` (date), `Budget` (nullable, ≤200 chars), `DrawnAt?`, `CancelledAt?`.
- *Why*: memberships, circle badges, the people list and wishlist access keep working unchanged. A separate table keeps drawing-only columns off permanent circles.
- *Alternative*: a fully separate drawing aggregate with its own access rules. Rejected because every wishlist, reservation and buddy check would then need a second path.

### 2. Participants are separate from members
`DrawingParticipants`: `Id`, `GroupId`, `UserId?`, `Name`, `Email?`, `Status` (`Invited` | `Joined` | `Declined`), `InviteCode` (32-hex, unique), `AnsweredAt?`. A `GroupMember` row exists **only for Joined participants**.
- The draw, exclusions and assignments refer to participant ids, so people who haven't answered (including people without an account) take part.
- "Names only until joined" follows directly: an invited person isn't a member, so `SharesGroupAsync` doesn't link them through this drawing.
- The organizer manages through `Drawing.OrganizerId` and is a member only if they are also a participant. A non-participating organizer therefore sees nobody's wishlist through the drawing.

### 3. Adding participants and avoiding duplicates on the server
The create and add requests carry `circleIds` (with `excludedUserIds`), `userIds` and `{name, email}` entries. The server:
1. expands circles into user ids, keeping only circles the organizer is in and dropping excluded users;
2. keeps only user ids the organizer shares a circle with;
3. looks up each typed email; if it belongs to an account, it becomes that user (`UserId` set) but keeps the **typed name** on the participant;
4. drops duplicates by user id and by normalized email.

The organizer's screens show typed participants by their typed name until they join, so nothing on screen depends on whether an account exists. Silent merging means a typed email that matches someone already in the list simply disappears into the existing entry. Accepted: the organizer typed that address themself.

At most 100 participants per drawing.

### 4. Invitations: a personal code per participant
Every participant gets an email. Participants with a `UserId` also see an in-app card, and the drawing sits at the top of their circles list until they answer. The email link is `/join/{code}` using the participant's `InviteCode`:
- signed in → join or decline that participant (only if the signed-in user is that participant, or has no other participant in the drawing and the participant has no `UserId` yet; joining then sets `UserId`);
- not signed in → the existing join page offers sign in or register. Registration accepts a drawing code next to a `GroupInvitation` code: `InvitationResolver` grows a second lookup, and `RegistrationGate` returns a common "invite target" (circle invite or drawing participant).

A drawing code stays valid until the drawing is deleted, and it is personal, so once a participant has joined it can't be used again. Wrong codes count toward the existing per-IP limit (`InviteCodeGuard`).
- *Alternative*: reuse `CircleInvitation` for app users. Rejected because statuses, removal by the organizer and buddy joins would then be spread over two tables.

### 5. Joining on behalf of a friend through a buddy
`POST api/drawings/{id}/participants/{pid}/join` is allowed when the caller is that participant, **or** the caller is a Joined participant of the same drawing and is a buddy of the participant's `UserId` (`WishlistBuddy` with owner = participant user, buddy = caller). The participants list returns `canJoinForThem` so the button only appears where it works. This only joins. Declining stays with the person themself.

### 6. Visibility and secrecy
- Shared-circle queries (`SharesGroupAsync`, `GetCircleMatesAsync`, `GetUserGroupsAsync` for non-organizers) ignore drawings that are cancelled, or whose `EndDate` + 1 month has passed.
- The participants list (names, status, whether you're the organizer) is visible to the organizer and to every participant who hasn't declined. Emails are shown to the organizer only, for participants who haven't joined.
- `GET api/drawings/{id}/my-recipient` returns only the caller's own assignment. No endpoint returns other assignments. The site admin page doesn't show assignments.

### 7. Drawing names: a random single loop with backtracking
Shuffle the participants with `RandomNumberGenerator`, then search with depth-first backtracking for a Hamiltonian cycle in the "may give to" graph (everyone except yourself and your exclusion partners). Candidates are tried in random order, and the search stops after a fixed number of steps (for example 200 000). A full or ended search means "no valid draw", so nothing is saved and the organizer is told that the exclusions allow no draw. With at most 100 participants and realistic exclusions (pairs), a cycle is found almost immediately. The draw is idempotent under concurrency: `DrawnAt` is set in the same transaction, guarded by `WHERE DrawnAt IS NULL`.
- *Alternative*: any derangement (pairs swapping allowed). Rejected because pairs of people who simply swap make guessing easier, and #154's repair relies on a single loop.

### 8. Leaving or declining after the draw
A participant (Invited or Joined) can still decline or leave after the draw: their status becomes Declined and their membership is removed. Assignments are not changed (that's #154). The organizer's email names who declined **and** who no longer has a Secret Santa (the decliner's recipient), so the organizer can sort it out personally. This reveals one assignment (decliner → recipient) to the organizer. That was a deliberate choice by the user: being able to fix the situation matters more than keeping that one pair secret. The person who drew the decliner gets an email that no gift is needed for them. The decliner's recipient stays without a giver in the app until #154.

### 9. Cancel, undo and cleanup
Cancelling sets `CancelledAt` and emails every participant who hasn't declined. The drawing disappears for participants (circles list, Wishlists page, cards, wishlist access). The organizer still sees it, subdued, with **Undo**, until cleanup. Undo clears `CancelledAt` and emails the same people that it's back on.

`ProcessReservationsUseCase`'s job runs a new `CleanUpDrawingsUseCase` after the reservations. It deletes drawings with `CancelledAt` or `EndDate` more than one month ago (cascade: members, participants, exclusions, assignments, the group). Reservations are left alone. Hiding is based on dates, so the app hides a drawing exactly on time whenever the job happens to run.

### 10. Emails and language
New email types: invitation, your recipient (sent after the draw), cancelled, back on, no gift needed (to the decliner's giver) and someone declined (to the organizer). Language: the recipient's saved language, otherwise the organizer's (the organizer's actions cause all of these), otherwise English. If sending fails, the action still goes through, as with existing invitation emails.

### 11. API shape (BFF)
`POST api/drawings` (create), `GET api/drawings/{id}`, `POST api/drawings/{id}/participants` (add), `DELETE api/drawings/{id}/participants/{pid}` (remove, before the draw), `PUT api/drawings/{id}/exclusions`, `POST api/drawings/{id}/draw` (body `confirmUnanswered: true` required when someone hasn't answered, otherwise 409 with the list), `POST …/participants/{pid}/join|decline`, `POST api/drawings/{id}/cancel|undo-cancel`, `GET api/drawings/{id}/my-recipient`, `GET api/drawing-invitations/me`. `groups/me` gains `kind` and a `drawing` summary (end date, organizer, my status, drawn, ended, cancelled). The Wishlists page gets the people the caller drew from `GET api/drawings/recipients/me` and `GET api/drawings/invitations/me` feeds the Join / Decline cards. `POST api/invitations/{code}/decline` lets someone decline from the email link without an account.

## Risks / Trade-offs

- [A decline after the draw leaves someone without a giver] → The organizer is told who declined and who is now without a giver, and handles it personally until #154 fixes it properly.
- [That email reveals one assignment to the organizer] → Accepted by the user. It only happens after a decline, and only by email, never in the app.
- [Backtracking blows up with dense exclusions] → Step cap plus a clear message. Participants are capped at 100.
- [The organizer's screens could leak who has an account] → Keep showing the typed name until they join, give the same response for both cases, and test it.
- [A personal drawing code lives for weeks (until deletion)] → It is single-person and leads only to that drawing. It is covered by the per-IP limit on wrong codes, and once the participant has joined it can't be used by anyone else.
- [Cleanup is monthly, so data stays in the database for 1–2 months after hiding] → Hiding is based on dates, and the data is ours to keep for that time.
- [Wishlist access through a drawing ends abruptly when it is hidden] → Intended. Reservations made in the meantime stay.

## Migration Plan

One additive EF migration: `Groups.Kind` (default `Circle`), plus the new tables `Drawings`, `DrawingParticipants`, `DrawingExclusions` and `DrawingAssignments`, with cascades from `Groups`. Existing rows are untouched. Rollback: redeploying the previous image still works, because the new columns and tables are ignored. The job picks up cleanup automatically on the next deploy.

## Open Questions

- Release note wording (feature) to be agreed with the user before the PR.
