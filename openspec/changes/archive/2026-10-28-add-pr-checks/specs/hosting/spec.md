## ADDED Requirements

### Requirement: Pull requests are checked before merging
Every pull request to `main` SHALL run the API tests, including the Postgres tests against a throwaway database, plus lint and a build of the web app. The checks SHALL use no secrets and no cloud resources.

#### Scenario: A change breaks a test
- **WHEN** a pull request makes an API test, a Postgres test, lint or the web build fail
- **THEN** the pull request shows a failed check before it is merged

#### Scenario: Checks can't reach production
- **WHEN** the checks run
- **THEN** they have a read-only repository token and no Azure login or production secrets

### Requirement: Workflow actions are pinned and kept current
Workflows SHALL reference every third-party action by full commit SHA, with the release version as a comment. Dependabot SHALL propose updates as pull requests: weekly for actions, and monthly for the web app's npm packages and the API's NuGet packages. Minor and patch updates SHALL be grouped per ecosystem.

#### Scenario: A tag is moved to malicious code
- **WHEN** an action's owner (or an attacker) moves a version tag
- **THEN** the workflows keep running the pinned commit until an update pull request is reviewed and merged

#### Scenario: A new action release
- **WHEN** a pinned action publishes a newer release
- **THEN** Dependabot opens a pull request that moves the SHA and its version comment, and the pull request checks run on it
