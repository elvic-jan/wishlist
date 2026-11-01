## ADDED Requirements

### Requirement: Password fields can be shown
Every password field on the sign-in, sign-up and account recovery pages SHALL have a button inside the field that shows or hides the typed password. The password SHALL be hidden by default. Each field SHALL toggle on its own. The button SHALL NOT submit the form, and SHALL be labelled for screen readers ("Show password" / "Hide password"). The browser's own reveal button (Edge) SHALL be hidden so only one button shows.

#### Scenario: Show the password while signing in
- **WHEN** a person types a password on the sign-in page and clicks the eye button
- **THEN** the password is shown as plain text and the button's label becomes "Hide password"
- **AND** the form is not submitted

#### Scenario: Hide it again
- **WHEN** the password is shown and the person clicks the button again
- **THEN** the password is masked again

#### Scenario: Recover fields toggle separately
- **WHEN** a person on the recovery page shows the "New password" field
- **THEN** the "Confirm password" field stays masked

#### Scenario: Only one reveal button in Edge
- **WHEN** the page is opened in Microsoft Edge
- **THEN** Edge's built-in reveal button is not shown, only the app's eye button
