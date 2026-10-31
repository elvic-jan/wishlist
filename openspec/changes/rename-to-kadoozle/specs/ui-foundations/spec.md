## MODIFIED Requirements

### Requirement: Product name and circle terminology
The application SHALL present itself to users as "Kadoozle" and SHALL call a group of people who share wishlists a "circle" in every user-facing text, including page content, navigation, page titles, user-visible API error messages and emails. The former name "GiftCircles" SHALL appear only where the move itself is explained. Internal names (code, API routes, database tables, browser storage keys) MAY keep the words "group", "wishlist" or "giftcircles".

#### Scenario: App name in the shell and browser tab
- **WHEN** a user opens any page of the application
- **THEN** the header and the browser tab title show "Kadoozle", and not "GiftCircles" or "Wishlist"

#### Scenario: Former name not shown
- **WHEN** a user reads any page, notice or email of the application
- **THEN** it does not say "GiftCircles", except in the message that explains the move

#### Scenario: Users are told about the move
- **WHEN** a user who had an account before the move signs in to Kadoozle for the first time
- **THEN** a message explains that GiftCircles is now Kadoozle at kadoozle.app, that passkeys and home-screen installs need to be set up again, and offers to add a passkey; the message goes away once they act on it or dismiss it

#### Scenario: Circles in navigation and pages
- **WHEN** a signed-in user views the navigation, the home page or a circle's page
- **THEN** the text refers to "circles" (for example "My circles", "Create circle", "Leave circle") and not to "groups"

#### Scenario: Circle page address
- **WHEN** a user opens a circle
- **THEN** the address is `/circles/{id}`

#### Scenario: Old circle address still works
- **WHEN** a user opens a bookmarked `/groups/{id}` address
- **THEN** the application redirects to `/circles/{id}`

#### Scenario: Error messages use the new word
- **WHEN** the API rejects a request because the user is not in the circle
- **THEN** the error message the user sees refers to a "circle"

#### Scenario: Emails use the product name
- **WHEN** the system sends any email (confirmation, password reset or change, passkey added, invitation, buddy, reservation reminder or expiry)
- **THEN** it comes from "Kadoozle" <noreply@kadoozle.app>, its subject and body refer to "Kadoozle", and its links point to https://www.kadoozle.app

### Requirement: Installable as a home-screen app
The application SHALL be installable from the browser as a standalone home-screen app on Android, iPhone/iPad and desktop browsers that support it, without an app store, and SHALL offer people a way to install it from inside the app.

#### Scenario: Manifest and icons are served
- **WHEN** a browser loads any page
- **THEN** the page links a web app manifest naming the app "Kadoozle", with standalone display, start URL `/`, theme and background colours, and icons of at least 192 and 512 pixels including a maskable icon, all served successfully

#### Scenario: iPhone home-screen icon
- **WHEN** someone adds the app to the home screen in Safari on iPhone or iPad
- **THEN** the home-screen icon is the Kadoozle icon and the title is "Kadoozle"

#### Scenario: Icons are filled to the edge
- **WHEN** any of the app's icon images is shown
- **THEN** its background colour reaches every edge, with no white or lighter lines along a side

#### Scenario: System bars readable in dark mode
- **WHEN** the installed app runs on Android with the phone or the app in dark mode
- **THEN** the system navigation bar is not white, and its buttons stay visible

#### Scenario: Browser colour follows the app's mode
- **WHEN** the user picks Light, Dark or System in the app
- **THEN** the page's `theme-color` and `color-scheme` match the resolved mode, not only the phone's setting

#### Scenario: Install from the account menu on Android or desktop
- **WHEN** the browser reports that the app can be installed and the user chooses "Install app" in the account menu
- **THEN** the browser's own install dialog opens

#### Scenario: Install help on iPhone
- **WHEN** a user on iPhone or iPad Safari chooses "Install app"
- **THEN** the app explains how to add it with Share → Add to Home Screen

#### Scenario: No install option when already installed
- **WHEN** the app is running as an installed app, or the browser can neither install it nor is iPhone/iPad Safari
- **THEN** no "Install app" option is shown

### Requirement: The app picks up new versions after a release
The application SHALL check whether a newer version is deployed when it starts, when it returns to the foreground or comes back online, and every 15 minutes while it is open; a check that gets no answer SHALL be retried after 10 s, 30 s, 1 min and then every 5 min while the app is open, and when there is one SHALL show a lasting "New version available" notice with a Refresh button, in the style of the message toasts. The notice SHALL say what the new version brings, using the release note of the deployed version. A release note SHALL only be shown with the release it was written for: when a version has no release note, or kept the note of the version before it, the notice SHALL say "Refresh to get the latest Kadoozle." The notice SHALL NOT offer a way to put it off or close it. Refresh SHALL load the new version, not the previously cached one. The application SHALL NOT reload by itself.

#### Scenario: Notice after a release
- **WHEN** a user opens the app, or returns to it, after a new version was deployed
- **THEN** a "New version available" notice appears with a Refresh button and no Not now or close button

#### Scenario: Notice says what's new
- **WHEN** the deployed version has the release note "Security fixes"
- **THEN** the notice shows "Security fixes" under its title

#### Scenario: Release without a note
- **WHEN** the deployed version has no release note
- **THEN** the notice shows "Refresh to get the latest Kadoozle."

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
- **THEN** its notice shows "Refresh to get the latest Kadoozle.", not the previous release's note

#### Scenario: Reminder on pull requests
- **WHEN** a pull request changes the app but not the release note
- **THEN** its checks show a warning (not a failure) saying the release will show the generic text

#### Scenario: First check gets no answer
- **WHEN** the installed app opens before its network is back, or while the server is still waking up, so the first check fails
- **THEN** the app checks again by itself within seconds and shows the notice once the server answers, without the user leaving or reloading the app

#### Scenario: App left open
- **WHEN** a new version is deployed while the app stays open in the foreground
- **THEN** the notice appears within 15 minutes
