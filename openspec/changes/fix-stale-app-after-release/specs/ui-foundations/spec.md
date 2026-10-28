## ADDED Requirements

### Requirement: The app picks up new versions after a release
The application SHALL check whether a newer version is deployed when it starts and when it returns to the foreground, and when there is one SHALL show a lasting "New version available" notice with Refresh and Not now, in the style of the message toasts. Refresh SHALL load the new version, not the previously cached one. The application SHALL NOT reload by itself.

#### Scenario: Notice after a release
- **WHEN** a user opens the app, or returns to it, after a new version was deployed
- **THEN** a "New version available" notice appears with Refresh and Not now

#### Scenario: Refresh loads the new version
- **WHEN** the user chooses Refresh
- **THEN** the page reloads and runs the newly deployed version, even when the service worker had cached the previous one

#### Scenario: Not now
- **WHEN** the user chooses Not now or closes the notice
- **THEN** the notice is hidden for the rest of this app session and the app keeps running the current version

#### Scenario: No notice when up to date
- **WHEN** the deployed version is the one running
- **THEN** no notice is shown

#### Scenario: Server unreachable or slow
- **WHEN** the check can't reach the server or gets no answer
- **THEN** nothing is shown, the app keeps working with the version it has, and a later check tries again

#### Scenario: Notice on sign-in pages
- **WHEN** a new version is deployed while someone is on a sign-in page
- **THEN** the notice is shown there too

#### Scenario: Messages wait for the notice
- **WHEN** the update notice is shown
- **THEN** message toasts are not shown at the same time
