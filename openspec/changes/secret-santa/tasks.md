## 1. Domain and database

- [x] 1.1 Add `GroupKind` (`Circle` | `Drawing`) to `Group` (default `Circle`)
- [x] 1.2 Add entities `Drawing` (GroupId PK, OrganizerId, Title via Group.Name, EndDate, Budget?, DrawnAt?, CancelledAt?), `DrawingParticipant` (Id, GroupId, UserId?, Name, Email?, Status, InviteCode, AnsweredAt?), `DrawingExclusion` (GroupId, ParticipantAId, ParticipantBId; stored ordered) and `DrawingAssignment` (GroupId, GiverParticipantId, ReceiverParticipantId)
- [x] 1.3 Add domain helpers: `Drawing.IsHidden(now)`, `IsEnded(now)`, visibility = EndDate/CancelledAt + 1 month; input limits for title, budget, name, email
- [x] 1.4 EF configuration, unique indexes (InviteCode; participant per user per drawing; normalized email per drawing), cascades from `Groups`, and one additive migration

## 2. Repositories and access rules

- [x] 2.1 `IDrawingRepository` + implementation (load drawing with participants, exclusions; own assignment; due-for-cleanup query; guarded `MarkDrawn` with `WHERE DrawnAt IS NULL`)
- [x] 2.2 Make `SharesGroupAsync`, `GetCircleMatesAsync` and `GetUserGroupsAsync` ignore hidden drawings (cancelled, or > 1 month past end date), and add Postgres tests for each
- [x] 2.3 Return drawing kind/status in `GetUserGroupsAsync`, plus drawings the user organizes without being a member and unanswered drawings for invited users

## 3. Creating and managing a drawing

- [x] 3.1 `CreateDrawingUseCase`: validate input, create group + drawing, add organizer as participant unless left out, then add participants (3.2)
- [x] 3.2 `AddDrawingParticipantsUseCase`: expand circles minus excluded users, filter to circles the organizer is in and circle-mates, resolve typed emails to accounts while keeping the typed name, de-duplicate by user id and normalized email, cap 100, reject after draw; create invite codes; send invitations (3.6)
- [x] 3.3 `RemoveDrawingParticipantUseCase` (organizer, before draw; removes membership and exclusions; no email)
- [x] 3.4 `SetDrawingExclusionsUseCase` (organizer, before draw; validate pairs)
- [x] 3.5 `GetDrawingUseCase`: participants with name/status, `canJoinForThem` per participant, emails for organizer only, typed name until joined; 404 for outsiders and declined non-organizers; never other people's assignments
- [x] 3.6 `DrawingEmails`: invitation, your recipient, cancelled, back on, no gift needed, someone declined; recipient language → organizer language → English; failures logged, not thrown

## 4. Joining and declining

- [x] 4.1 `AnswerDrawingInvitationUseCase`: join (create membership, set UserId when joining via code) and decline/leave (remove membership, status Declined); after the draw send the "someone declined" email to the organizer (naming the decliner and the recipient now without a Secret Santa) and the "no gift needed" email to the decliner's giver
- [x] 4.2 Join on behalf: allow when the caller is a Joined participant and a buddy of the target's user; join only
- [x] 4.3 `ListMyDrawingInvitationsUseCase` for the in-app cards
- [x] 4.4 Drawing invite codes: extend `InvitationResolver`/`RegistrationGate` with a drawing-participant target so `/join/{code}` check, join and register work; register joins the participant in the same transaction; keep `InviteCodeGuard` counting wrong codes; reject codes of removed participants, cancelled or deleted drawings and participants bound to another account

## 5. Drawing names

- [x] 5.1 `SecretSantaDrawer`: secure shuffle + backtracking Hamiltonian cycle respecting exclusions with a step cap; unit tests (valid loop, exclusions respected, impossible case with 3 people and one pair, randomness over many runs, 100 participants fast)
- [x] 5.2 `DrawNamesUseCase`: organizer only, ≥3 non-declined, 409 with unanswered list unless confirmed, guarded single draw, save assignments, send "your recipient" emails (wishlist link for joined, join link otherwise)
- [x] 5.3 `GetMyRecipientUseCase`: only the caller's own assignment, with recipient name, joined flag, user id when joined, budget, end date

## 6. Cancel, undo and cleanup

- [x] 6.1 `CancelDrawingUseCase` and `UndoCancelDrawingUseCase` (organizer only; emails to non-declined participants)
- [x] 6.2 `CleanUpDrawingsUseCase` deleting drawings whose end date or cancellation is more than a month ago; run it in `--job reservations` after the reservations, with its count in the job's log line and failure exit code
- [x] 6.3 Update infra/README's job description

## 7. API

- [x] 7.1 `DrawingsController` with the endpoints from design §11, mapped to the use cases, with DTOs in `Application/DTOs/DrawingDtos.cs`
- [x] 7.2 `groups/me` returns kind, end date, ended/cancelled flags and my status; invitation-link and circle-invite endpoints reject drawings with 400
- [x] 7.3 `GET api/drawings/recipients/me` returns the people the caller drew (drawing id + title, name, user id only when openable, joined); the Wishlists page puts them on top and leaves them out of the list below
- [x] 7.4 Server-side error/messages texts in English and Dutch

## 8. Web app

- [x] 8.1 Create-drawing dialog from My circles: title, end date, optional budget, participants picker (circles with untickable members, circle-mates, name + email rows, organizer ticked by default), exclusion pairs
- [x] 8.2 Drawing page `/drawings/:id`: participants with status badges, buddy "join for them", organizer tools (add, remove, exclusions, Draw names with unanswered confirmation dialog, Cancel/Undo), "your recipient" card, invitee Join/Decline, Leave
- [x] 8.3 My circles: unanswered drawings on top with Join/Decline, drawing marker, subdued ended/cancelled drawings, invitation cards
- [x] 8.4 Wishlists page: drawn people on top under "My wishlist" with the drawing title badge, not-joined state, no duplicates
- [x] 8.5 Join and register pages accept drawing codes (drawing title and organizer shown, prefilled name/email)
- [x] 8.6 English and Dutch texts for all new UI in `lib/i18n/en.ts` and `nl.ts`; dates formatted for the active language; `translate="no"` on drawing title, budget, names and emails; i18n check passes
- [x] 8.7 Wording review: every new English and Dutch text reviewed; "Secret Santa" renamed to "Drawing names / Namen trekken" (event: "drawing" / "trekking")

## 9. Tests

- [x] 9.1 Scenario tests: `SecretSantaTests` (draw algorithm, visibility dates) and `SecretSantaPostgresTests` (full flow on Postgres; the in-memory API gets a `NoDrawings` stand-in)
- [x] 9.2 Postgres tests for access filtering, guarded draw, cleanup cascade and migration
- [x] 9.3 Test that the organizer's responses are identical for a typed email with and without an account
- [x] 9.4 Web tests: the web app has no test runner; checked in the browser instead (create dialog, invite link + sign-up, draw confirmation, recipient card, Wishlists order, decline from link, cancel/undo)

## 10. Release

- [x] 10.1 Agree the release note text (type `feature`, English + `nl:`) with the user and add `release-notes/name-drawings.md`
- [ ] 10.2 Check on staging: create, invite by email, register via link, buddy join, draw with unanswered people, recipient email/card, Wishlists order, cancel/undo, cleanup job run
- [x] 10.3 PR with "Closes #101"

## 11. Staging feedback

- [x] 11.1 Circles list: all drawings above the permanent circles (unanswered ones first)
- [x] 11.2 Circle page: a badge with the drawing's title on each member you drew
- [x] 11.3 Release note: the second line of `release-notes/name-drawings.md` extended, agreed with the user
- [ ] 11.4 Check on staging, then archive the change
