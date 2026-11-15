## ADDED Requirements

### Requirement: Drawings in the circles list
The circles list SHALL include the Secret Santa drawings the user organizes or takes part in, marked as drawings. Drawings SHALL appear above the permanent circles, with the ones the user hasn't answered first, and opening an unanswered one SHALL offer Join or Decline. Drawings the user declined SHALL NOT appear, except to their organizer. Ended drawings in their final month, and cancelled drawings (shown to the organizer only), SHALL appear in a subdued style. Drawings that are hidden (cancelled for other users, or more than a month past their end date) SHALL NOT appear.

#### Scenario: Unanswered drawing on top
- **WHEN** a user with the circles Family and Friends has an unanswered invitation to "Christmas 2026"
- **THEN** "Christmas 2026" is shown above Family and Friends, and opening it offers Join or Decline

#### Scenario: Ended drawing
- **WHEN** a drawing the user joined ended last week
- **THEN** it is still listed, in a subdued style

#### Scenario: Cancelled drawing
- **WHEN** the organizer has cancelled a drawing
- **THEN** participants no longer see it, and the organizer sees it subdued with Undo

### Requirement: Drawings are not permanent circles
A drawing SHALL NOT offer the actions of permanent circles: no invitation links, no "Invite people from my circles" and no leave-circle action. Participants SHALL instead join, decline or leave through the drawing. Only active drawings SHALL count as shared circles: a cancelled drawing or one more than a month past its end date SHALL NOT give access to wishlists, reservations or buddy choices.

#### Scenario: No invite link for a drawing
- **WHEN** a member requests an invitation link for a drawing
- **THEN** the system returns 400 Bad Request

#### Scenario: Access ends with the drawing
- **WHEN** Anna and Piet share only a drawing that was cancelled
- **THEN** Anna can no longer open Piet's wishlist, and Piet no longer appears in her people list
