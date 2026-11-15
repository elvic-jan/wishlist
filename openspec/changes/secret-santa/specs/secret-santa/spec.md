## ADDED Requirements

### Requirement: Create a drawing
The system SHALL let any signed-in user create a Secret Santa drawing with a title (required, at most 200 characters), an end date (required, today or later) and an optional budget text (at most 200 characters). The creator SHALL become the drawing's organizer. The drawing SHALL be a temporary circle whose names have not been drawn yet.

#### Scenario: Drawing created
- **WHEN** a signed-in user creates a drawing titled "Christmas 2026" with an end date of 24 December and budget "€30"
- **THEN** the system creates the drawing with that user as organizer and returns it, not yet drawn

#### Scenario: Budget left empty
- **WHEN** a user creates a drawing without a budget
- **THEN** the drawing is created without a budget, and no budget is mentioned anywhere

#### Scenario: Missing end date or title
- **WHEN** a user creates a drawing without an end date, with an end date in the past, or with an empty title
- **THEN** the system returns 400 Bad Request and creates nothing

#### Scenario: Not signed in
- **WHEN** a request to create a drawing carries no valid session
- **THEN** the system returns 401 Unauthorized

### Requirement: Add participants
Before the draw, the organizer SHALL be able to add participants from whole circles they belong to (with the option to leave out individual members), from people they share a circle with, and as a name plus email address. When a drawing is created, the organizer SHALL be a participant by default and SHALL be able to leave themself out. The system SHALL merge duplicates silently: the same account or email address SHALL appear only once, whichever way it was added. A typed email address that belongs to an existing account SHALL become that account's participant, and the system's responses and the organizer's screens SHALL be the same whether or not the address has an account. A drawing SHALL hold at most 100 participants.

#### Scenario: Circle added with a member left out
- **WHEN** the organizer adds their circle Family and leaves out Grandpa
- **THEN** every member of Family except Grandpa becomes a participant with status Invited

#### Scenario: Same person through two ways
- **WHEN** the organizer adds the circles Family and Friends, which both contain Anna, and also selects Anna individually
- **THEN** Anna is a participant once

#### Scenario: Typed email of someone already added
- **WHEN** the organizer types the email address of a person who is already a participant
- **THEN** no second participant is created and the organizer is not told which participant it matched

#### Scenario: Typed email with an existing account
- **WHEN** the organizer types the name "Piet" and an email address that belongs to an existing account
- **THEN** that account becomes the participant, the organizer sees "Piet" with status Invited exactly as for an address without an account, and Piet gets the in-app invitation as well as the email

#### Scenario: Circle the organizer is not in
- **WHEN** the request includes a circle the organizer is not a member of, or a user they share no circle with
- **THEN** those entries are ignored

#### Scenario: Organizer leaves themself out
- **WHEN** the organizer unticks themself while creating the drawing
- **THEN** the organizer manages the drawing but is not a participant and is not part of the draw

#### Scenario: Too many participants
- **WHEN** adding would bring the drawing above 100 participants
- **THEN** the system returns 400 Bad Request and adds nobody

#### Scenario: Adding after the draw
- **WHEN** the organizer tries to add participants after names have been drawn
- **THEN** the system returns 409 Conflict and adds nobody

#### Scenario: Not the organizer
- **WHEN** anyone other than the organizer tries to add participants
- **THEN** the system returns 403 Forbidden

### Requirement: Remove participants before the draw
The organizer SHALL be able to remove any participant until names have been drawn. A removed participant SHALL lose access to the drawing, their invitation SHALL stop working, exclusions involving them SHALL be removed, and they SHALL NOT be notified.

#### Scenario: Participant removed
- **WHEN** the organizer removes Mia before the draw
- **THEN** Mia is no longer a participant, her invitation card disappears, her invitation link no longer works, and no email is sent

#### Scenario: Removing after the draw
- **WHEN** the organizer tries to remove a participant after the draw
- **THEN** the system returns 409 Conflict and changes nothing

### Requirement: Exclusions
Before the draw, the organizer SHALL be able to set exclusions as pairs of participants. An exclusion SHALL work in both directions: neither person in the pair can draw the other.

#### Scenario: Partners excluded
- **WHEN** the organizer excludes the pair Jan and Sofie and draws names
- **THEN** Jan does not draw Sofie and Sofie does not draw Jan

#### Scenario: Pair with a non-participant
- **WHEN** an exclusion names someone who is not a participant of the drawing, or the same person twice
- **THEN** the system returns 400 Bad Request

### Requirement: Invitation to a drawing
Every participant SHALL receive an email inviting them to the drawing, naming the organizer, the drawing's title, its end date and the budget if there is one, with a personal link. Participants with an account SHALL also see an invitation card with Join and Decline in the app, and unanswered drawings SHALL appear at the top of their circles list. Opening one SHALL offer the same choice. A visitor without an account who follows the link SHALL be able to register and join in one step. The personal link SHALL work only for that participant, until the drawing is deleted. Wrong codes SHALL count toward the existing per-IP limit on invitation codes.

#### Scenario: Join from the card
- **WHEN** an invited participant with an account presses Join on the invitation card
- **THEN** their status becomes Joined and the drawing becomes one of their circles

#### Scenario: Decline from the card
- **WHEN** an invited participant presses Decline
- **THEN** their status becomes Declined, the card and the drawing disappear from their circles list, and nobody is notified (before the draw)

#### Scenario: Register through the link
- **WHEN** a visitor without an account opens their personal link and registers
- **THEN** their account is created and their participant status becomes Joined as part of the same registration

#### Scenario: Link already used by another account
- **WHEN** someone signs in with a different account than the one the participant already belongs to and opens that participant's link
- **THEN** the system returns 404 Not Found and changes nothing

#### Scenario: Link of a removed participant or a deleted drawing
- **WHEN** someone opens the link of a removed participant, or of a drawing that is cancelled or deleted
- **THEN** the system treats the code as not found

#### Scenario: Email failure
- **WHEN** the invitation email can't be sent
- **THEN** the participant is still added

### Requirement: Join on behalf of a friend
A participant who has joined SHALL be able to join on behalf of another invited participant whose wishlist they are a buddy for, by choosing that person's name in the participants list. They SHALL NOT be able to decline on that person's behalf.

#### Scenario: Parent joins for their child
- **WHEN** Jan, a buddy for Mia's wishlist, has joined and taps "Mia · Invited"
- **THEN** Mia's status becomes Joined

#### Scenario: Not a buddy
- **WHEN** a joined participant who is not a buddy for Mia tries to join on her behalf
- **THEN** the system returns 403 Forbidden and Mia stays Invited

#### Scenario: Buddy has not joined yet
- **WHEN** Jan is still Invited himself and tries to join for Mia
- **THEN** the system returns 403 Forbidden

### Requirement: Participants and their status
The organizer and every participant who has not declined SHALL see the drawing's participants with their names and status (Joined, Invited or Declined). A participant's wishlist SHALL be visible to another participant through the drawing only when both have joined. People who already share a permanent circle SHALL keep seeing each other's wishlists as before. Participants who were added by email and haven't joined SHALL be shown by the name the organizer typed. Only the organizer SHALL see their email addresses.

#### Scenario: Statuses shown
- **WHEN** a participant opens the drawing where Anna has joined, Piet hasn't answered and Mia has declined
- **THEN** the list shows Anna · Joined, Piet · Invited and Mia · Declined

#### Scenario: Names only until joined
- **WHEN** Anna, who has joined, and Piet, who hasn't, share no permanent circle
- **THEN** Anna sees Piet's name but can't open his wishlist, and after Piet joins she can

#### Scenario: Not a participant
- **WHEN** a user who is neither the organizer nor a participant requests the drawing
- **THEN** the system returns 404 Not Found

### Requirement: Draw names
The organizer SHALL be able to draw names once at least 3 participants have not declined. Declined participants SHALL be left out. If some participants haven't answered, the system SHALL list them and require the organizer's confirmation, and then SHALL include them. The draw SHALL form a single loop through all included participants, in which nobody draws themself or their exclusion partner. If no such loop exists, the system SHALL draw nothing and tell the organizer that the exclusions don't allow a draw. After the draw, the participant list and exclusions SHALL be fixed. Names SHALL be drawn only once.

#### Scenario: Everyone has answered
- **WHEN** the organizer presses Draw names with 5 joined participants
- **THEN** each of them is assigned exactly one recipient, the assignments form one loop of 5, and the drawing becomes drawn

#### Scenario: Unanswered participants
- **WHEN** the organizer presses Draw names while Piet and Mia haven't answered
- **THEN** the system lists Piet and Mia and draws nothing until the organizer confirms, after which Piet and Mia are included

#### Scenario: Too few participants
- **WHEN** fewer than 3 participants have not declined
- **THEN** the system returns 400 Bad Request and draws nothing

#### Scenario: Exclusions make it impossible
- **WHEN** the exclusions leave no possible single loop (for example 3 participants of whom two are an excluded pair)
- **THEN** the system draws nothing and tells the organizer that no draw is possible with these exclusions

#### Scenario: Drawn twice
- **WHEN** the organizer (or two of their devices at once) presses Draw names on a drawing that is already drawn
- **THEN** the system returns 409 Conflict and the existing assignments stay unchanged

#### Scenario: Not the organizer
- **WHEN** anyone other than the organizer tries to draw names
- **THEN** the system returns 403 Forbidden

### Requirement: Your recipient
After the draw, every included participant SHALL receive an email naming the person they drew, the drawing's title, the end date and the budget if there is one. The email SHALL link to the recipient's wishlist for participants who have joined, or to their personal join link for those who haven't. The app SHALL show the same information on a "your recipient" card. Each participant SHALL be able to see only their own recipient. Nobody, including the organizer, SHALL be able to see other assignments in the app. The only exception is the organizer's email after a decline following the draw.

#### Scenario: Recipient shown
- **WHEN** Anna has drawn Piet and opens the drawing
- **THEN** she sees a card saying she is Piet's Secret Santa, with the budget and a link to Piet's wishlist

#### Scenario: Recipient hasn't joined
- **WHEN** Anna has drawn Piet and Piet hasn't joined yet
- **THEN** the card says Piet hasn't joined yet and offers no wishlist link

#### Scenario: Unanswered participant gets the result
- **WHEN** Mia hadn't answered at the draw
- **THEN** her email names her recipient and links to her personal join link, and she sees her recipient's wishlist once she has joined

#### Scenario: Organizer can't see assignments
- **WHEN** the organizer requests the drawing or any participant's details
- **THEN** no response contains anyone's recipient other than the organizer's own

### Requirement: Declining or leaving after the draw
A participant SHALL still be able to decline or leave after the draw. Their status SHALL become Declined, they SHALL lose access through the drawing, and the assignments SHALL stay unchanged. The organizer SHALL receive an email naming who declined and who no longer has a Secret Santa (the person the decliner had drawn), so the organizer can sort it out personally. This SHALL be the only way the organizer learns anything about the assignments. The participant who drew the decliner SHALL receive an email saying that no gift is needed for that person.

#### Scenario: Unanswered participant declines after the draw
- **WHEN** Piet, who had drawn Anna and hadn't answered, declines after the draw
- **THEN** Piet's status becomes Declined, the organizer is emailed that Piet declined and that Anna no longer has a Secret Santa, and the person who drew Piet is emailed that no gift is needed for Piet

#### Scenario: Organizer declines after the draw
- **WHEN** the organizer is a participant and declines or leaves after the draw
- **THEN** the organizer still receives the email naming who no longer has a Secret Santa

### Requirement: Cancel and undo
The organizer SHALL be able to cancel a drawing at any time before it is deleted. Every participant who hasn't declined SHALL receive an email, and the drawing SHALL disappear for participants, including its cards, its place on the Wishlists page and any wishlist access through it. The organizer SHALL still see the cancelled drawing, in a subdued style, and SHALL be able to undo the cancellation until cleanup. Undoing it SHALL restore everything as it was and email the same participants that the drawing is back on.

#### Scenario: Cancelled
- **WHEN** the organizer cancels a drawn drawing
- **THEN** participants are emailed, no longer see the drawing or their recipient, and can't open co-participants' wishlists through it

#### Scenario: Undo
- **WHEN** the organizer undoes the cancellation two weeks later
- **THEN** the drawing, its participants, statuses and assignments are back as they were, and the participants are emailed that it is back on

#### Scenario: Not the organizer
- **WHEN** anyone other than the organizer tries to cancel or undo
- **THEN** the system returns 403 Forbidden

### Requirement: End date and cleanup
After its end date, a drawing SHALL stay visible to its organizer and participants in a subdued style for one month, and SHALL then be hidden. The monthly maintenance job SHALL permanently delete drawings whose end date or cancellation is more than one month ago, together with their participants, exclusions, assignments and memberships. Wishes and reservations SHALL NOT be deleted.

#### Scenario: Just ended
- **WHEN** a participant opens My circles a week after the drawing's end date
- **THEN** the drawing is shown in a subdued style and their recipient is still visible

#### Scenario: Hidden after a month
- **WHEN** more than a month has passed since the end date and the job hasn't run yet
- **THEN** the drawing no longer appears anywhere and no longer gives wishlist access

#### Scenario: Deleted by the job
- **WHEN** the monthly job runs and a drawing ended or was cancelled more than a month ago
- **THEN** the drawing and all its data are deleted permanently, and reservations made by its participants stay

### Requirement: Drawing emails use the right language
Every drawing email SHALL be written in the recipient's saved language, otherwise in the organizer's language, otherwise in English, including the subject and the dates. A failed email SHALL NOT block the action that caused it.

#### Scenario: Stranger invited by a Dutch organizer
- **WHEN** a Dutch-speaking organizer invites an email address without an account
- **THEN** the invitation email is in Dutch

#### Scenario: Recipient's own language wins
- **WHEN** a Dutch-speaking organizer draws names and Anna's saved language is English
- **THEN** Anna's "your recipient" email is in English and its end date is formatted in English

### Requirement: Secret Santa texts in English and Dutch
Every text this feature adds SHALL be available in English and Dutch: the create-drawing dialog, the drawing page, the participant statuses, the invitation and recipient cards, the draw confirmation, the circles list and Wishlists page markers, the join and register screens for drawing links, the server's validation and error messages, and the emails. Dates SHALL be formatted for the active language. What people typed SHALL be shown as entered and marked as not to be translated by the browser: the drawing title, the budget, and participant names and email addresses.

#### Scenario: Drawing page in Dutch
- **WHEN** a person whose language is Dutch opens a drawing
- **THEN** all app texts on it are Dutch, such as the statuses "Doet mee", "Uitgenodigd" and "Afgemeld" and the button "Namen trekken", and the end date reads like "24 december 2026"

#### Scenario: Server error in Dutch
- **WHEN** a Dutch-speaking organizer tries to draw names with exclusions that make a draw impossible
- **THEN** the message explaining why is in Dutch

#### Scenario: Typed text untouched
- **WHEN** a Dutch-speaking person views the drawing "Christmas 2026" with budget "max €30"
- **THEN** the title and budget are shown exactly as typed, with `translate="no"`

#### Scenario: Missing Dutch text caught
- **WHEN** a Secret Santa text has no Dutch translation
- **THEN** the build or the tests fail

### Requirement: Drawn people marked in circles
In the member list of a permanent circle, each person the user drew in an active drawing SHALL carry a badge with the drawing's title, one badge per drawing. Other members SHALL NOT get such a badge, and the badge SHALL NOT reveal who drew whom to anyone else.

#### Scenario: Recipient in a shared circle
- **WHEN** Anna drew Peter in "Christmas 2026" and opens their circle Family
- **THEN** Peter's row in Family shows a "Christmas 2026" badge, and nobody else's row does

#### Scenario: Others see no badge
- **WHEN** Piet, who did not draw Peter, opens Family
- **THEN** Peter's row shows no drawing badge
