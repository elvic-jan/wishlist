## Why

To add someone you already know from another circle, you have to make an invite link and send it to them through WhatsApp or email, even though they already have an account. Inviting them right inside the app is quicker, and it can't go to the wrong person.

## What Changes

- The **Invite people** menu on a circle gets a new entry, **"People from my circles"**. It opens a picker:
  - People are **grouped by circle**, and each group can be **collapsed**.
  - A **filter** box narrows the list by name.
  - It's **multi-select**: tick several people, or a whole circle at once, then press **Invite**.
  - Someone in several of your circles appears in each group. Ticking them in one group ticks them everywhere, and they get one invitation.
  - People already in the circle aren't listed. People already invited are shown as "Invited" and can't be ticked again.
- Any **member** of a circle can invite this way, the same as with invite links.
- You can only invite people who **share at least one circle with you**.
- The invited person **accepts first**:
  - They get an **email** ("Olivia invited you to join Family").
  - They see the invitation on **My circles** with **Join** and **Decline** buttons.
  - Joining makes them a member. Declining removes the invitation, and they can be invited again later.
- An invitation stays open until it's answered. It stops working if its circle is deleted.
- Out of scope: inviting by email address or account search, limiting who can invite (that belongs to the circle admin role, to-do #12), and reminders for unanswered invitations.

## Capabilities

### New Capabilities
- `circle-invites`: inviting existing users from your circles into a circle, the invite picker's data, the invitee's pending invitations, joining or declining, and the email.

### Modified Capabilities
_None._ Invite links (`groups`) work exactly as before.

## Impact

- **Domain**: new `CircleInvitation` entity (GroupId, InviteeId, InvitedById, CreatedAt), unique per circle and invitee.
- **Infrastructure**: EF Core migration (new table), repository, and an email template.
- **Application**: use cases for invite candidates, inviting people, listing my invitations, joining and declining.
- **API**:
  - `GET /api/groups/{groupId}/invite-candidates`
  - `POST /api/groups/{groupId}/circle-invitations`
  - `GET /api/circle-invitations/me`
  - `POST /api/circle-invitations/{id}/accept`
  - `POST /api/circle-invitations/{id}/decline`
- **Web**: a new menu entry and picker dialog on the circle page, and an "Invitations" card on My circles.
