## MODIFIED Requirements

### Requirement: The app picks up new versions after a release
The application SHALL check whether a newer version is deployed when it starts, when it returns to the foreground or comes back online, and every 15 minutes while it is open; a check that gets no answer SHALL be retried after 10 s, 30 s, 1 min and then every 5 min while the app is open, and when there is one SHALL show a lasting notice "A new version of Kadoozle is available" with a Refresh button and a What's new button, in the style of the message toasts. The notice SHALL NOT show a release note itself. What's new SHALL load the new version the way Refresh does and then open the What's new page in it. The notice SHALL NOT offer a way to put it off or close it. Refresh SHALL load the new version, not the previously cached one. The application SHALL NOT reload by itself.

#### Scenario: Notice after a release
- **WHEN** a user opens the app, or returns to it, after a new version was deployed
- **THEN** a notice "A new version of Kadoozle is available" appears with Refresh and What's new buttons and no Not now or close button

#### Scenario: What's new opens the new version's notes
- **WHEN** the user chooses What's new on the notice
- **THEN** the page reloads into the newly deployed version and opens the What's new page, which lists that version's notes at the top

#### Scenario: Refresh loads the new version
- **WHEN** the user chooses Refresh
- **THEN** the page reloads and runs the newly deployed version, even when the service worker had cached the previous one

#### Scenario: Several releases between visits
- **WHEN** two or more versions were deployed since the user last opened the app, and the files of the version they last saw are gone from the server
- **THEN** the app still opens (never a blank page) and shows the notice, and Refresh loads the newest version

#### Scenario: No notice when up to date
- **WHEN** the deployed version is the one running
- **THEN** no notice is shown

#### Scenario: Server unreachable or slow
- **WHEN** the check can't reach the server or gets no answer
- **THEN** nothing is shown, the app keeps working with the version it has, and a later check tries again

#### Scenario: Notice on sign-in pages
- **WHEN** a new version is deployed while someone is on a sign-in page
- **THEN** the notice is shown there too, and What's new opens the What's new page without requiring sign-in

#### Scenario: Messages wait for the notice
- **WHEN** the update notice is shown
- **THEN** message toasts are not shown until the new version is loaded

#### Scenario: First check gets no answer
- **WHEN** the installed app opens before its network is back, or while the server is still waking up, so the first check fails
- **THEN** the app checks again by itself within seconds and shows the notice once the server answers, without the user leaving or reloading the app

#### Scenario: App left open
- **WHEN** a new version is deployed while the app stays open in the foreground
- **THEN** the notice appears within 15 minutes
