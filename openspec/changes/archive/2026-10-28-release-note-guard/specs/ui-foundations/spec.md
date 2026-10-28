## MODIFIED Requirements

### Requirement: The app picks up new versions after a release
The application SHALL check whether a newer version is deployed when it starts and when it returns to the foreground, and when there is one SHALL show a lasting "New version available" notice with a Refresh button, in the style of the message toasts. The notice SHALL say what the new version brings, using the release note of the deployed version. A release note SHALL only be shown with the release it was written for: when a version has no release note, or kept the note of the version before it, the notice SHALL say "Refresh to get the latest GiftCircles." The notice SHALL NOT offer a way to put it off or close it. Refresh SHALL load the new version, not the previously cached one. The application SHALL NOT reload by itself.

#### Scenario: Notice after a release
- **WHEN** a user opens the app, or returns to it, after a new version was deployed
- **THEN** a "New version available" notice appears with a Refresh button and no Not now or close button

#### Scenario: Notice says what's new
- **WHEN** the deployed version has the release note "Security fixes"
- **THEN** the notice shows "Security fixes" under its title

#### Scenario: Release without a note
- **WHEN** the deployed version has no release note
- **THEN** the notice shows "Refresh to get the latest GiftCircles."

#### Scenario: Refresh loads the new version
- **WHEN** the user chooses Refresh
- **THEN** the page reloads and runs the newly deployed version, even when the service worker had cached the previous one

#### Scenario: Several releases between visits
- **WHEN** two or more versions were deployed since the user last opened the app, and the files of the version they last saw are gone from the server
- **THEN** the app still opens (never a blank page) and shows the notice with the newest version's release note, and Refresh loads the newest version

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
- **THEN** message toasts are not shown until the new version is loaded

#### Scenario: Release that didn't write a note
- **WHEN** a version is deployed whose release note is the same as the live version's (for example a dependency update)
- **THEN** its notice shows "Refresh to get the latest GiftCircles.", not the previous release's note

#### Scenario: Reminder on pull requests
- **WHEN** a pull request changes the app but not the release note
- **THEN** its checks show a warning (not a failure) saying the release will show the generic text
