## ADDED Requirements

### Requirement: Open-source licences are credited
The application SHALL credit the open-source libraries it ships to the browser with their licence notices, on a page that anyone can open and that is linked from the account menu.

#### Scenario: Licences page lists bundled libraries
- **WHEN** someone opens the "Open-source licences" page
- **THEN** it names Lucide as the source of the logo and icons, and lists every npm package included in the web app's bundle with its version, licence name and full licence text

#### Scenario: List follows the build
- **WHEN** the web app is built
- **THEN** the list is generated from the packages actually in the bundle, and the build fails if one of them has no licence

#### Scenario: Reachable from the account menu
- **WHEN** a signed-in user opens the account menu
- **THEN** it has an "Open-source licences" item under "What's new" that opens the page

#### Scenario: Open without signing in
- **WHEN** someone who is not signed in opens `/licenses`
- **THEN** the page is shown on the sign-in layout instead of redirecting to sign-in
