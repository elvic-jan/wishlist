## MODIFIED Requirements

### Requirement: Installable as a home-screen app
The application SHALL be installable from the browser as a standalone home-screen app on Android, iPhone/iPad and desktop browsers that support it, without an app store, and SHALL offer people a way to install it from inside the app.

#### Scenario: Manifest and icons are served
- **WHEN** a browser loads any page
- **THEN** the page links a web app manifest naming the app "Kadoozle", with id `/`, standalone display, start URL `/wishlists`, theme and background colours, and icons of at least 192 and 512 pixels including a maskable icon, all served successfully

#### Scenario: iPhone home-screen icon
- **WHEN** someone adds the app to the home screen in Safari on iPhone or iPad
- **THEN** the home-screen icon is the Kadoozle icon and the title is "Kadoozle"

#### Scenario: Icons are filled to the edge
- **WHEN** the maskable icon or the iPhone home-screen icon is shown
- **THEN** its background colour reaches every edge, with no white or lighter lines along a side

#### Scenario: General icons have rounded corners
- **WHEN** one of the general-purpose ("any") icons is shown, for example on an older Android startup screen or a desktop install
- **THEN** it is a terracotta tile with rounded, transparent corners and no white or lighter lines along a side

#### Scenario: Startup screen without a faint border
- **WHEN** the installed app starts on Android
- **THEN** the startup screen background is a deeper terracotta than the icon, so the icon's edge reads as intended rather than as a faint border

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
