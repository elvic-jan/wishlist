## Context

Today people join a circle only through invite links (`GroupInvitation`): a member creates a code that works for 3 days, sends it outside the app, and anyone with the link can join. Memberships live in `GroupMembers`. Emails go through `IEmailSender`, in the same style as the buddy and reservation emails. The circle page's `InviteMenu` is a dropdown that creates a link as soon as it opens.

## Goals / Non-Goals

**Goals:**
- From a circle, invite existing users you know from your other circles, several at once.
- The invitee decides: Join or Decline, by email and in the app.

**Non-Goals:**
- Inviting by email address or searching all accounts (that would reveal who has an account).
- Limiting who may invite. That waits for the circle admin role (to-do #12).
- Expiry or reminders for unanswered invitations.

## Decisions

**1. A separate `CircleInvitation` table, not a variant of `GroupInvitation`.**
Link invitations are codes anyone can use, are shared, and expire. A direct invitation is addressed to one user and waits for their answer. Mixing the two would make every link query and the code guard deal with null codes. The table has Id, GroupId (cascade delete with the circle), InviteeId and InvitedById (FK to users; cascade when the invitee's account goes, `SET NULL` for the inviter), and CreatedAt. It's unique on (GroupId, InviteeId), so a person has at most one open invitation per circle.

**2. Joining or declining deletes the row.**
There's no status column. A row means "waiting for an answer", which keeps the "already invited?" check and the invitee's list trivial. Declining is silent: the inviter isn't told, and the person can be invited again.

**3. Candidates come from the server, already grouped.**
`GET /groups/{id}/invite-candidates` returns `[{ groupId, name, members: [{ userId, displayName, invited }] }]`. It covers the caller's other circles in name order, members in name order. It leaves out the caller, people already in the target circle, and circles left empty. The client handles filtering, collapsing and selection. *Alternative:* a flat list of people with their circle names, grouped by the client. The grouped response matches the screen directly and keeps the "who can I invite" rule in one place.

**4. Inviting several people in one request.**
`POST /groups/{id}/circle-invitations` takes `{ userIds: [...] }` (at most 100). For each person, the server checks that the caller is a member of the target circle (otherwise 403 for the whole request) and that the person shares a circle with the caller. It skips people who are already members or already invited. It returns `{ invited, skipped }` counts and emails each newly invited person, best-effort. If none of the ids are allowed, the request returns 403. If some are, it invites those and counts the rest as skipped, so one stale tick doesn't block the others.

**5. Joining reuses the membership code.**
Accept checks that the invitation belongs to the caller (404 otherwise, so other people's invitation ids reveal nothing). It adds a Member role membership if they aren't one yet, then deletes the row. Membership and deletion happen in one transaction (`IUnitOfWork`).

**6. Web.**
- **InviteMenu**: a new item, "People from my circles", above the link options. It opens `InviteFromCirclesDialog`.
- **Picker dialog**:
  - A filter input (case-insensitive name match). While filtering, circles with no match are hidden and the others are expanded.
  - One section per circle with a chevron to collapse it, a tri-state "select all" checkbox, and a selected count.
  - People rows with a checkbox; "Invited" people are disabled.
  - Selection is a set of user ids, so ticking someone shows as ticked in every circle they appear in.
  - The footer has "Invite (n)", and a toast confirms the result.
  - Native checkboxes styled with the theme's accent color, so no new dependency.
- **My circles**: an "Invitations" card at the top when there are any, one row per invitation ("Olivia invited you to Family") with **Join** and **Decline**. Joining reloads the circles list.

## Risks / Trade-offs

- [Someone keeps re-inviting a person who declined] → Every invitation needs a shared circle, and declining is one tap. Revisit (e.g. a cool-down) if it happens.
- [An invitation stays open forever] → It's harmless: it disappears when answered or when the circle is deleted. Expiry can be added later.
- [Big families have long lists] → The filter box, collapsible circles and per-circle "select all" keep it manageable.

## Migration Plan

One EF migration adds the `CircleInvitations` table, with no changes to existing data. It deploys with the normal pipeline. To roll back, revert the app; the table is ignored.

## Open Questions

- None blocking.
