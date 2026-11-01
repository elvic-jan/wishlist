## ADDED Requirements

### Requirement: Wishlists is the default page
The SPA SHALL open the Wishlists page (`/wishlists`) as its default page and show "My circles" at `/circles`. Opening `/`, an unknown path, the product wordmark in the app shell, or the installed app SHALL land on Wishlists.

#### Scenario: Opening the site root
- **WHEN** a signed-in user opens `/`
- **THEN** the SPA shows the Wishlists page at `/wishlists`

#### Scenario: Clicking the wordmark
- **WHEN** a signed-in user clicks the Kadoozle wordmark in the app shell
- **THEN** the SPA opens `/wishlists`

#### Scenario: My circles has its own address
- **WHEN** a signed-in user opens `/circles`
- **THEN** "My circles" is shown

### Requirement: Top menu order and highlighting
The app shell's top menu SHALL show "Wishlists" first and "Circles" second. "Circles" SHALL open `/circles` and SHALL be highlighted on `/circles` and on every circle page `/circles/{id}`, and not on wishlist pages.

#### Scenario: Menu order
- **WHEN** a signed-in user views the top menu
- **THEN** "Wishlists" comes before "Circles"

#### Scenario: Highlighted on a circle page
- **WHEN** the user is on `/circles` or `/circles/{id}` (including after an old `/groups/{id}` link redirects there)
- **THEN** "Circles" is highlighted in the top menu and "Wishlists" is not

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

### Requirement: Back returns to the previous screen
Back buttons on wishlist, circle and import pages SHALL return to the screen the user came from within the app. When there is no previous screen in the app, Back on a wishlist page or the import page SHALL go to Wishlists, and Back on a circle page SHALL go to "My circles".

#### Scenario: Back after opening a wishlist from a circle
- **WHEN** a user opens a member's wishlist from a circle page and presses Back
- **THEN** the circle page is shown again

#### Scenario: Back on a wishlist page opened directly
- **WHEN** a user opens a wishlist page directly from a link and presses Back
- **THEN** the Wishlists page is shown

#### Scenario: Back on a circle page opened directly
- **WHEN** a user opens a circle page directly from a link and presses Back
- **THEN** "My circles" is shown
