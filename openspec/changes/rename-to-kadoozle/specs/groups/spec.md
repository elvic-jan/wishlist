## MODIFIED Requirements

### Requirement: Invite link preview
When an invite link is shared in a messaging app, the page it points to SHALL carry link-preview tags naming the inviter and the circle, with the Kadoozle preview image. Any other page, and any invite code that can't be used or is rate-limited, SHALL carry generic Kadoozle preview tags, so a preview never confirms a guessed code.

#### Scenario: Personal preview
- **WHEN** a link-preview crawler fetches a valid, unexpired invite link
- **THEN** the page's preview title is "<inviter> invited you to <circle> on Kadoozle", with the Kadoozle preview image at an absolute URL

#### Scenario: Generic preview for unusable codes
- **WHEN** a crawler fetches an invite link that is unknown, expired, or comes from a client that is rate-limited for wrong codes
- **THEN** the page carries the generic Kadoozle preview and reveals no circle or inviter

#### Scenario: Page still works
- **WHEN** a person opens an invite link in a browser
- **THEN** they get the normal app, which shows "<inviter> invited you to <circle>" on the join page
