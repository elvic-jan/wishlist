## 1. Domain and database

- [x] 1.1 `CircleInvitation` entity (Id, GroupId, InviteeId, InvitedById, CreatedAt)
- [x] 1.2 EF Core config and migration: `CircleInvitations` table, unique (GroupId, InviteeId), FK to Groups (cascade), invitee (cascade), inviter (`SET NULL`)

## 2. Application

- [x] 2.1 `ICircleInvitationRepository`: candidates for a circle (grouped, with the invited flag), open invitations for the circle, create several (skipping existing), list mine (with circle and inviter names), get by id, delete
- [x] 2.2 `GetInviteCandidatesUseCase` (member check, 403)
- [x] 2.3 `InviteFromCirclesUseCase`: 1–100 ids, member check, shared-circle check per id, skip members and already invited, 403 when none allowed, email each new invitee best-effort, return counts
- [x] 2.4 Invitation email (plain text + HTML) linking to the app's home
- [x] 2.5 `GetMyCircleInvitationsUseCase`, `AnswerCircleInvitationUseCase` (join in a transaction / decline; 404 for someone else's)

## 3. API

- [x] 3.1 `GET /api/groups/{groupId}/invite-candidates`, `POST /api/groups/{groupId}/circle-invitations`
- [x] 3.2 `GET /api/circle-invitations/me`, `POST /api/circle-invitations/{id}/accept`, `POST /api/circle-invitations/{id}/decline`

## 4. Tests

- [x] 4.1 Rules (in memory): candidates grouping and exclusions, invite counts and skips, 403s, 400s, one email per new invitee, email failure, join/decline/someone else's
- [x] 4.2 Postgres: candidate query, unique index, cascade on circle delete, join in a transaction

## 5. Web

- [x] 5.1 "People from my circles" in Invite people, opening the picker dialog
- [x] 5.2 Picker: filter, collapsible circles, per-circle select all (tri-state), multi-select shared across circles, "Invited" rows disabled, Invite (n), toast
- [x] 5.3 My circles: Invitations card with Join / Decline
- [x] 5.4 Check on phone width and in dark mode
