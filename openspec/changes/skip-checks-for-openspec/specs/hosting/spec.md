## MODIFIED Requirements

### Requirement: Pull requests are checked before merging
Every pull request to `main` SHALL run the API tests, including the Postgres tests against a throwaway database, plus lint and a build of the web app. The checks SHALL use no secrets and no cloud resources. A pull request that only changes `openspec/` SHALL skip the tests and build, and still be mergeable when they are required checks.

#### Scenario: A change breaks a test
- **WHEN** a pull request makes an API test, a Postgres test, lint or the web build fail
- **THEN** the pull request shows a failed check before it is merged

#### Scenario: Checks can't reach production
- **WHEN** the checks run
- **THEN** they have a read-only repository token and no Azure login or production secrets

#### Scenario: OpenSpec-only pull request
- **WHEN** a pull request only changes files under `openspec/` (an archive or a proposal)
- **THEN** the tests and build are skipped, the required checks count as passed, and the pull request can be merged
