## MODIFIED Requirements

### Requirement: Release notes are written per pull request
Each pull request that changes the app SHALL be able to describe itself in its own file under `release-notes/`, with a `type` of `feature`, `fix` or `major` and one or more one-line notes of at most 200 characters. Each note SHALL be written in English and followed by its translation in every other language the app offers, on a line starting with that language's code and a colon (for example `nl: …`), also at most 200 characters. Pull request checks SHALL warn (not fail) when a pull request changes the app without adding a note file, and SHALL fail when a note file is malformed or a note lacks a translation.

#### Scenario: Parallel pull requests
- **WHEN** two pull requests each add a note file and both are merged
- **THEN** neither has a merge conflict over release notes, and the next release lists both notes

#### Scenario: Missing note
- **WHEN** a pull request changes app code and adds no file under `release-notes/`
- **THEN** its checks show a warning that the release will ship without a note

#### Scenario: Malformed note
- **WHEN** a note file has an unknown type, no note text, or a line longer than 200 characters
- **THEN** the pull request's checks fail

#### Scenario: Note without a Dutch line
- **WHEN** a note file has an English note line that is not followed by an `nl:` line
- **THEN** the pull request's checks fail

### Requirement: What's new page
The app SHALL have a What's new page at `/whats-new` listing every release from 1.0.0 on, newest first, each with its version, release date and notes. Notes SHALL be shown in the reader's language, falling back to English for a note that has no translation. A release without notes SHALL show "Small fixes and improvements" in the reader's language. Translations added later to the note files of earlier releases SHALL appear for those releases. The page SHALL be reachable from the account menu and from the update notice, and SHALL open without signing in.

#### Scenario: Viewing the history
- **WHEN** a user opens What's new after releases 1.0.0 and 1.1.0
- **THEN** 1.1.0 is listed first and 1.0.0 second, each with its date and notes

#### Scenario: No notes before 1.0.0
- **WHEN** a user opens What's new
- **THEN** nothing older than 1.0.0 is listed

#### Scenario: From the account menu
- **WHEN** a signed-in user opens the menu under their initials and chooses What's new
- **THEN** the What's new page opens

#### Scenario: Notes in Dutch
- **WHEN** a Dutch-language person opens What's new
- **THEN** each note is shown in Dutch, including notes of releases made before translations existed
