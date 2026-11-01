## ADDED Requirements

### Requirement: Releases have semantic version numbers
Every production release SHALL have a version number MAJOR.MINOR.PATCH. The first numbered release SHALL be 1.0.0. Each later version SHALL be computed from the previous release's version and the release note files added since: any `major` note bumps MAJOR, otherwise any `feature` note bumps MINOR, otherwise PATCH is bumped (also when there are no notes). A version SHALL be assigned only to a deploy that reaches production; changes from deploys that were never approved SHALL count towards the next version. Version numbers SHALL NOT be stored in `package.json` or project files.

#### Scenario: First release
- **WHEN** no release has been tagged yet and a deploy reaches production
- **THEN** it is version 1.0.0

#### Scenario: Feature release
- **WHEN** the last release is 1.0.0 and the changes since add one `feature` note and one `fix` note
- **THEN** the next release is 1.1.0

#### Scenario: Release without notes
- **WHEN** the last release is 1.1.0 and the changes since add no notes (for example a dependency update)
- **THEN** the next release is 1.1.1

#### Scenario: Deploy not approved
- **WHEN** a build was never approved and a later build for a newer commit is deployed
- **THEN** the deployed release's version and notes include the changes of both

### Requirement: Release notes are written per pull request
Each pull request that changes the app SHALL be able to describe itself in its own file under `release-notes/`, with a `type` of `feature`, `fix` or `major` and one or more one-line notes of at most 200 characters. Pull request checks SHALL warn (not fail) when a pull request changes the app without adding a note file, and SHALL fail when a note file is malformed.

#### Scenario: Parallel pull requests
- **WHEN** two pull requests each add a note file and both are merged
- **THEN** neither has a merge conflict over release notes, and the next release lists both notes

#### Scenario: Missing note
- **WHEN** a pull request changes app code and adds no file under `release-notes/`
- **THEN** its checks show a warning that the release will ship without a note

#### Scenario: Malformed note
- **WHEN** a note file has an unknown type, no note text, or a line longer than 200 characters
- **THEN** the pull request's checks fail

### Requirement: Production releases are tagged and published
After a production deploy succeeds, the deployed commit SHALL be tagged `vX.Y.Z`, a GitHub Release `vX.Y.Z` with the release's notes SHALL be created, and the deployed image SHALL also carry the tag `X.Y.Z`. Re-deploying an earlier release (rollback) SHALL NOT create or move tags.

#### Scenario: Successful deploy
- **WHEN** the deploy of version 1.2.0 passes its smoke check
- **THEN** the commit is tagged `v1.2.0`, a GitHub Release `v1.2.0` lists its notes, and the image has the tag `1.2.0`

#### Scenario: Failed deploy
- **WHEN** migrations or the smoke check fail
- **THEN** no tag and no GitHub Release are created

#### Scenario: Rollback
- **WHEN** the deploy job of an earlier, already tagged release is re-run
- **THEN** that release is deployed again and no tag is created or changed

### Requirement: What's new page
The app SHALL have a What's new page at `/whats-new` listing every release from 1.0.0 on, newest first, each with its version, release date and notes. A release without notes SHALL show "Small fixes and improvements". The page SHALL be reachable from the account menu and from the update notice, and SHALL open without signing in.

#### Scenario: Viewing the history
- **WHEN** a user opens What's new after releases 1.0.0 and 1.1.0
- **THEN** 1.1.0 is listed first and 1.0.0 second, each with its date and notes

#### Scenario: No notes before 1.0.0
- **WHEN** a user opens What's new
- **THEN** nothing older than 1.0.0 is listed

#### Scenario: From the account menu
- **WHEN** a signed-in user opens the menu under their initials and chooses What's new
- **THEN** the What's new page opens

### Requirement: The running version is visible
The bottom of the account menu (the menu that opens from the user's initials at the top right) SHALL show the running version as "Version X.Y.Z". The account menu SHALL NOT mark new versions with a dot or badge. `GET /healthz` SHALL report the running version and commit without querying the database.

#### Scenario: Version in the menu
- **WHEN** a signed-in user running 1.0.0 opens the account menu
- **THEN** "Version 1.0.0" is shown at the bottom of the menu

#### Scenario: Health check reports the version
- **WHEN** the deploy's smoke check calls `GET /healthz` after deploying 1.0.0
- **THEN** the response is 200 and names version 1.0.0 and the deployed commit
