# ui-foundations Specification

## Purpose
Codifies the SPA's design-system foundations: how the visual style is
expressed in tokens, how theme and color mode persist across reloads, the
shape of the persistent app shell that hosts every authenticated page, and
the accessibility minimums every page is expected to meet. Future "theme
picker" / "micro-frontend host" / "tenant branding" changes will modify or
extend this capability.
## Requirements
### Requirement: Visual styling is driven exclusively by theme tokens

All visual styling in the SPA SHALL be expressed through a fixed set of semantic theme tokens — `background`, `foreground`, `card`, `card-foreground`, `popover`, `popover-foreground`, `primary`, `primary-foreground`, `secondary`, `secondary-foreground`, `muted`, `muted-foreground`, `accent`, `accent-foreground`, `destructive`, `destructive-foreground`, `border`, `input`, `ring`. Component code SHALL NOT reference raw colors (hex literals, named CSS colors, or palette utilities such as `bg-orange-700`); only the token sheet (the active theme's CSS variable block) is permitted to use raw color values.

#### Scenario: A page renders entirely from token-based classes

- **WHEN** any page is rendered
- **THEN** every visible color comes from a CSS variable defined by the active theme (e.g. `hsl(var(--primary))`), and no component file in `src/Wishlist.Web/src/components/` or `src/Wishlist.Web/src/pages/` contains a raw hex color, a named CSS color (other than `transparent` / `currentColor`), or a Tailwind palette utility (such as `bg-zinc-100`, `text-orange-700`)

#### Scenario: A new theme is added without touching component code

- **WHEN** a new theme is registered (a `.theme-<id>` CSS class with the full token sheet + its `.theme-<id>.dark` companion + an entry in the theme registry)
- **THEN** the new theme can be activated by switching the `theme-<id>` class on the root element with no other change to component code

### Requirement: Theme and color mode are independent and persisted

The system SHALL treat **theme** (which set of tokens is active) and **mode** (light vs. dark vs. system) as two orthogonal user preferences. Both SHALL be persisted in browser local storage and SHALL be applied as classes on the `<html>` element. When mode is "system", the system SHALL track the operating system's `prefers-color-scheme` preference and SHALL react to changes in that preference while the tab is open.

#### Scenario: User selects a mode and the choice persists across reloads

- **WHEN** a user selects Light, Dark, or System from the mode toggle in the app shell
- **THEN** the system writes the selection to `localStorage`, updates the `<html>` element's `dark` class to reflect the resolved mode, and on the next reload restores the same selection without any intermediate flash of the wrong mode

#### Scenario: System mode reacts to a live OS theme change

- **WHEN** mode is set to "System" and the operating system's color scheme preference changes (for example because the user toggled dark mode in their OS) while the SPA tab is open
- **THEN** the system updates the `<html>` element's `dark` class to match the new OS preference within the same event tick, without requiring a reload

#### Scenario: First paint is theme- and mode-correct

- **WHEN** the SPA loads in any state — first visit, returning visit with a persisted preference, or returning visit relying on system mode
- **THEN** the very first paint already reflects the correct theme class and mode class on `<html>`, with no flash of an alternate theme or mode before React mounts

### Requirement: Every authenticated page renders within a persistent app shell

The system SHALL render every authenticated route inside a single persistent app shell that carries the product wordmark, the mode toggle, and an authenticated user menu (avatar + display name + sign-out). Navigation between authenticated pages SHALL keep the shell mounted; only the page content area below the shell re-renders.

#### Scenario: Navigation between authenticated pages does not flicker the shell

- **WHEN** an authenticated user navigates from one authenticated page to another (for example from the home groups list to a group detail page)
- **THEN** the top navigation bar (wordmark, mode toggle, user menu) is not unmounted and re-mounted; only the content below the shell changes

#### Scenario: Unauthenticated pages render outside the shell

- **WHEN** an unauthenticated user lands on the login, register, forgot-password, reset-password, or join-by-invitation page
- **THEN** the page renders on a centered card layout without the authenticated top navigation, so a logged-out visitor never sees the mode toggle, the user menu, or the wordmark in the authenticated shell

### Requirement: Destructive actions use accessible dialogs, not native alerts

The system SHALL present every destructive confirmation through an accessible in-app dialog (with a named title, a description, a Cancel button, and a destructive-styled Confirm button) and SHALL NOT use the browser's native `window.confirm` or `window.alert` dialogs for any user-facing flow.

#### Scenario: Deleting a wishlist item requires confirmation in an accessible dialog

- **WHEN** an authenticated user clicks the delete control on one of their own wishlist items
- **THEN** the system opens an in-app dialog that names the item being deleted, explains that the action is irreversible, offers a Cancel button that closes the dialog without effect, and offers a destructive-styled Confirm button that performs the deletion only after explicit click

#### Scenario: Leaving a group requires confirmation in an accessible dialog

- **WHEN** an authenticated user clicks the leave-group control on a group detail page
- **THEN** the system opens an in-app dialog that names the group being left, explains the consequence, offers Cancel, and offers a destructive-styled Confirm

#### Scenario: No production code path invokes `window.confirm` or `window.alert`

- **WHEN** the SPA is built for production
- **THEN** no source file in `src/Wishlist.Web/src/` contains a call to `window.confirm` or `window.alert`

### Requirement: Interactive elements are keyboard-operable with visible focus

Every interactive element in the SPA (buttons, links, inputs, dialogs, dropdown menus) SHALL be reachable and operable via keyboard alone, and SHALL display a visible focus indicator when focused via keyboard.

#### Scenario: Tab order reaches every interactive element

- **WHEN** a user presses Tab repeatedly on any page
- **THEN** focus moves through every interactive element on the page in a logical order, and no interactive element is unreachable

#### Scenario: Focus ring is visible on keyboard focus

- **WHEN** an interactive element receives focus via keyboard (not mouse click)
- **THEN** a visible focus ring renders around the element using the active theme's `ring` token, with sufficient contrast against the surrounding surface

#### Scenario: Dialog traps focus while open

- **WHEN** a dialog is open
- **THEN** Tab and Shift+Tab cycle through focusable elements inside the dialog only, and Escape closes the dialog and returns focus to the element that opened it

### Requirement: Future themes register additively without changing application code

The system SHALL maintain a single source of truth — a theme registry — that lists every available theme with a stable identifier, a user-visible display name, and a representative swatch color. Adding a new theme to the registry, together with its CSS token block, SHALL be sufficient to make the theme selectable by future user-facing UI; no other application code change SHALL be required.

#### Scenario: A new theme registered today is selectable by future picker code

- **WHEN** a new entry is added to the theme registry and a corresponding `.theme-<id>` (plus `.theme-<id>.dark`) CSS block is added to the stylesheet
- **THEN** the new theme is activatable by setting `localStorage` to that theme id and reloading, without any change to `ThemeContext`, the boot script, the app shell, the routing layer, or any page component

#### Scenario: The current MVP ships exactly one theme but the registry is the contract

- **WHEN** the modernize-ui change initially ships
- **THEN** the theme registry contains exactly one entry (`terracotta` / "Earth") and the SPA renders that theme; a future "theme picker" change can ship by adding registry entries and rendering a picker UI, with no refactor of the existing surface

### Requirement: Product name and circle terminology
The application SHALL present itself to users as "GiftCircles" and SHALL call a group of people who share wishlists a "circle" in every user-facing text, including page content, navigation, page titles, user-visible API error messages and emails. Internal names (code, API routes, database tables) MAY keep the word "group".

#### Scenario: App name in the shell and browser tab
- **WHEN** a user opens any page of the application
- **THEN** the header and the browser tab title show "GiftCircles" and not "Wishlist"

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
- **WHEN** the system sends a password-reset or password-changed email
- **THEN** the sender name, subject and body refer to "GiftCircles"

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

### Requirement: Circle page layout
The circle page SHALL show the circle's name as its heading, list the members with the signed-in user first, let each member row open that member's wishlist (the user's own row opens their wishlist for editing), and list the other circles the user belongs to.

#### Scenario: Heading shows the circle name
- **WHEN** a member opens a circle
- **THEN** the page heading is the circle's name

#### Scenario: Yourself first, editable
- **WHEN** a member views the members of a circle
- **THEN** their own entry is listed first and opens their own wishlist for editing

#### Scenario: Member rows are single buttons
- **WHEN** a member views the members of a circle
- **THEN** each other member is shown as one button displaying their name that opens their wishlist

#### Scenario: Other circles
- **WHEN** a member who belongs to several circles opens one of them
- **THEN** the page lists the other circles they belong to, each opening that circle, and no longer shows a separate "Other wishlists" list

### Requirement: Back returns to the previous screen
Back buttons on wishlist and circle pages SHALL return to the screen the user came from within the app, and SHALL go to "My circles" when there is no previous screen in the app.

#### Scenario: Back after opening a wishlist from a circle
- **WHEN** a user opens a member's wishlist from a circle page and presses Back
- **THEN** the circle page is shown again

#### Scenario: Back on a page opened directly
- **WHEN** a user opens a wishlist page directly from a link and presses Back
- **THEN** "My circles" is shown

### Requirement: Lists don't show an empty state before loading
Pages that load a list SHALL show a loading placeholder until the data has arrived, and SHALL show an empty state only when the loaded list is actually empty.

#### Scenario: No empty-state flash
- **WHEN** a wishlist page is opened or returned to and its items are still loading
- **THEN** a loading placeholder is shown, not "No items yet" or "Nothing here yet"

#### Scenario: No "No circles yet" flash
- **WHEN** "My circles" is opened or returned to, for example with Back from a circle, and the circles are still loading
- **THEN** a loading placeholder is shown, not "No circles yet" or the "New circle" button

