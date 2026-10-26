## ADDED Requirements

### Requirement: Messages defined in code
The system SHALL support messages defined in application code, each with a stable id, title, text, an optional action (label and in-app link), an optional date window, and a rule deciding which users it applies to. The system SHALL return to a signed-in user the messages that are within their date window, apply to them, and that they haven't dismissed.

#### Scenario: Applicable message is returned
- **WHEN** a signed-in user requests their messages and a message applies to them, is within its date window and wasn't dismissed by them
- **THEN** the message is returned with its id, title, text and action

#### Scenario: Audience rule
- **WHEN** a message's rule doesn't apply to a user
- **THEN** that message isn't returned to them

#### Scenario: Date window
- **WHEN** a message's end date has passed, or its start date hasn't come yet
- **THEN** it isn't returned

### Requirement: Dismiss a message
The system SHALL let a user dismiss a message, or act on it, so that it is never shown to them again on any device.

#### Scenario: Dismissed for good
- **WHEN** a user dismisses a message, or uses its action
- **THEN** the message isn't returned to them again, on any device

#### Scenario: Dismissing twice
- **WHEN** a user dismisses a message they already dismissed
- **THEN** the system succeeds without changes

#### Scenario: Unknown message
- **WHEN** a user dismisses a message id that doesn't exist
- **THEN** the system returns 404 Not Found

### Requirement: Messages shown as lasting toasts
The application SHALL show a signed-in user's messages as toasts that stay until handled, one at a time, each offering its action (if any), "Remind me next time", and a close button. "Remind me next time" SHALL hide the message until the app is next opened, without dismissing it.

#### Scenario: One at a time
- **WHEN** a user has several messages
- **THEN** one is shown, and the next appears after the current one is handled

#### Scenario: Remind me next time
- **WHEN** a user chooses "Remind me next time"
- **THEN** the message disappears for the rest of this app session and is shown again the next time the app is opened

#### Scenario: Action
- **WHEN** a user chooses a message's action
- **THEN** the app goes to the action's page and the message is dismissed for good

### Requirement: Import and what's-new messages
The system SHALL include a message inviting users who haven't imported (when old data is loaded) to bring their wishes from the old site, with an action to the import page, and a "What's new" message for everyone describing recent features.

#### Scenario: Import message
- **WHEN** old data is loaded and a user hasn't imported
- **THEN** they get the "Bring your wishes from the old site" message with an "Import my wishes" action to the import page

#### Scenario: No import message after importing
- **WHEN** a user has imported
- **THEN** the import message isn't returned to them
