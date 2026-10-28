## MODIFIED Requirements

### Requirement: Messages shown as lasting toasts
The application SHALL show a signed-in user's messages as toasts that stay until handled, one at a time, each offering its action (if any), "Remind me next time", and a close button. "Remind me next time" SHALL hide the message until the app is next opened, without dismissing it. A message's action SHALL open its page in the newest deployed version of the app.

#### Scenario: One at a time
- **WHEN** a user has several messages
- **THEN** one is shown, and the next appears after the current one is handled

#### Scenario: Remind me next time
- **WHEN** a user chooses "Remind me next time"
- **THEN** the message disappears for the rest of this app session and is shown again the next time the app is opened

#### Scenario: Action
- **WHEN** a user chooses a message's action
- **THEN** the app goes to the action's page and the message is dismissed for good

#### Scenario: Action while a newer version is deployed
- **WHEN** a user chooses a message's action and a newer version of the app is deployed than the one running
- **THEN** the new version loads directly on the action's page, so the feature the message is about is there
