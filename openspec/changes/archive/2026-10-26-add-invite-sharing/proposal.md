## Why

To invite someone today, a member copies a bare link and pastes it into WhatsApp or an email themselves. In the chat it shows up as an anonymous URL with no preview, which looks like spam. Sharing should take one tap and arrive as a recognisable, personal invitation.

## What Changes

- The circle page's "Generate invite link" becomes **"Invite people"**, with these options:
  - **Share…** opens the device's own share sheet (WhatsApp, Signal, Messages, Mail, …) where the browser supports it.
  - **WhatsApp** opens WhatsApp with the message filled in, for browsers without a share sheet.
  - **Email** opens the user's own mail app with a subject and message filled in. GiftCircles sends no email itself.
  - **Copy link** works as before.
- The shared text is a friendly message rather than a bare link: "Jan invited you to the Family circle on GiftCircles, where we share wishlists. The link works for 3 days: https://…".
- Each "Invite people" creates a new link, as today. Links stay usable by several people until they expire.
- **BREAKING (behaviour):** new invitation links expire after **3 days** instead of 7. Links already sent keep their 7 days.
- **Link preview:** an invite link shared in WhatsApp, iMessage, Signal, Slack and similar apps shows "Jan invited you to Family on GiftCircles" with a wide GiftCircles image. Other pages get a generic GiftCircles preview.
- **Join page:** shows "Jan invited you to Family" before the visitor signs up or joins.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `groups`: invitation links expire after 3 days; the invitation check also returns the inviter's name and the circle's name; members can share an invitation through the share sheet, WhatsApp, email or copy; invite links have a personal link preview.

## Impact

- **Domain / infrastructure**: invitation lifetime 3 days (new links only). No schema change.
- **Application**: the invitation check returns the inviter's display name and the circle name; a small use case gives the link preview the same information.
- **API**: `GET /api/invitations/{code}` gets `inviterName` and `circleName`. The server answers `/join/{code}` with the SPA page plus per-invite preview tags, behind the existing invite-code rate limit.
- **Web**: an "Invite people" menu on the circle page; the join page shows who invited you to which circle; generic preview tags and a 1200×630 preview image.
- **Privacy**: anyone holding a valid invite link can see the inviter's name and the circle's name, which is what they see when joining anyway. Unknown, expired or rate-limited codes get the generic preview, so the preview never confirms a guessed code.
