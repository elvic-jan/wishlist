## MODIFIED Requirements

### Requirement: Installable as a home-screen app
The application SHALL be installable from the browser as a standalone home-screen app on Android, iPhone/iPad and desktop browsers that support it, without an app store, and SHALL offer people a way to install it from inside the app.

#### Scenario: Manifest and icons are served
- **WHEN** a browser loads any page
- **THEN** the page links a web app manifest naming the app "GiftCircles", with standalone display, start URL `/`, theme and background colours, and icons of at least 192 and 512 pixels including a maskable icon, all served successfully

#### Scenario: iPhone home-screen icon
- **WHEN** someone adds the app to the home screen in Safari on iPhone or iPad
- **THEN** the home-screen icon is the GiftCircles icon and the title is "GiftCircles"

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

### Requirement: Lists don't show an empty state before loading
Pages that load a list SHALL show a loading placeholder until the data has arrived, and SHALL show an empty state only when the loaded list is actually empty.

#### Scenario: No empty-state flash
- **WHEN** a wishlist page is opened or returned to and its items are still loading
- **THEN** a loading placeholder is shown, not "No items yet" or "Nothing here yet"

#### Scenario: No "No circles yet" flash
- **WHEN** "My circles" is opened or returned to, for example with Back from a circle, and the circles are still loading
- **THEN** a loading placeholder is shown, not "No circles yet" or the "New circle" button
