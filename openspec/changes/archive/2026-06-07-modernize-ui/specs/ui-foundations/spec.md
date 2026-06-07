## ADDED Requirements

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
