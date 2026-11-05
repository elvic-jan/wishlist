## ADDED Requirements

### Requirement: Staging runs separately from production
A staging environment SHALL run the same image and infrastructure template as production, in its own Azure resource group, scaling to zero, at its own free `azurecontainerapps.io` address. It SHALL share no database, credentials, secrets, Azure identity or passkey domain with production. Its database SHALL be a separate PostgreSQL server, and SHALL only ever hold test data, never a copy of production data.

#### Scenario: Passkeys are separate
- **WHEN** a passkey is created on staging
- **THEN** it belongs to staging's own hostname and is never offered on `kadoozle.app`, and production passkeys are never offered on staging

#### Scenario: Staging credentials cannot reach production
- **WHEN** a workflow job runs in the `staging` GitHub environment
- **THEN** its Azure sign-in can change only staging's resource group, and none of its secrets give access to the production database

### Requirement: Every main build is deployed to staging
Every build of `main` SHALL be deployed to staging automatically, without approval: database migrations first, then the new image for the app and the reservation job, then a smoke check that `/healthz` reports the new version. Staging deploys SHALL use their own concurrency group, separate from production's.

#### Scenario: Push to main
- **WHEN** a commit lands on `main` and the build succeeds
- **THEN** it is deployed to staging and the smoke check confirms the new version

#### Scenario: Staging deploy doesn't cancel a waiting production deploy
- **WHEN** a production deploy is waiting for approval and a staging deploy starts
- **THEN** the production deploy keeps waiting

### Requirement: Pull requests can be deployed to staging on demand
The owner SHALL be able to deploy any branch of the repository to staging by starting the deploy workflow by hand on that branch. Such a run SHALL never deploy to production, tag a release or create a GitHub Release, and its version SHALL be marked as a pre-release of the branch's commit. Deploys SHALL NOT be triggered by pull request events from forks, comments or labels.

#### Scenario: Deploying a branch
- **WHEN** the owner runs the deploy workflow on branch `feat/x`
- **THEN** that branch's build is deployed to staging with a version such as `1.4.0-pr.abc1234`, and no production job runs

#### Scenario: Outsiders cannot deploy
- **WHEN** someone without write access opens a pull request or comments on one
- **THEN** no staging or production deploy starts

### Requirement: Staging's database is reset only when its migrations don't match
Before migrating, a staging deploy SHALL compare the migrations already applied to the staging database with those in the build. When the database has a migration the build doesn't contain, the deploy SHALL reset the database to the clean seeded test data and then apply the build's migrations. Otherwise it SHALL only apply new migrations and keep existing test data.

#### Scenario: Normal main deploy keeps test data
- **WHEN** staging runs `main` and the next `main` build adds a migration
- **THEN** the migration is applied and existing test accounts and circles remain

#### Scenario: Back to main after a pull request with a migration
- **WHEN** a branch with an extra migration was deployed to staging, and then a `main` build without that migration is deployed
- **THEN** the database is reset to the seeded test data and migrated to `main`'s schema before the new image starts

### Requirement: Staging has seeded test data
A seeding job in the same image SHALL create a small set of test users, circles, wishes and a reservation, with email addresses on staging's allow-list. It SHALL refuse to run unless the environment is marked as a test environment, and SHALL NOT print passwords.

#### Scenario: Seeding a fresh database
- **WHEN** the seeding job runs on an empty, migrated staging database
- **THEN** the test users can sign in and see their circles and wishes

#### Scenario: Seeding refused in production
- **WHEN** the seeding job is started without a test environment label
- **THEN** it exits with an error and changes nothing

### Requirement: Test environments email only allow-listed recipients
When the app runs with an environment label (a test environment), it SHALL send email only to recipients on its configured allow-list, and SHALL prefix the subject of every sent email with `[TEST] `. An allow-list entry SHALL be an exact address, which also matches the same address with any `+tag` before the `@`, or `*@domain` for a whole domain, compared without regard to case. Mail to any other recipient SHALL be dropped and logged with its subject and a masked recipient, never its body or links. If the allow-list is empty or missing, no email SHALL be sent. Without an environment label (production), email SHALL be sent as before.

#### Scenario: Listed address
- **WHEN** the allow-list contains `tester@example.com` and the app emails `tester@example.com`
- **THEN** the email is sent with the subject starting `[TEST] `

#### Scenario: Plus-variant of a listed address
- **WHEN** the allow-list contains `tester@example.com` and the app emails `Tester+alice@example.com`
- **THEN** the email is sent

#### Scenario: Whole domain
- **WHEN** the allow-list contains `*@example.org` and the app emails `anyone@example.org`
- **THEN** the email is sent

#### Scenario: Unlisted address is dropped and masked in the log
- **WHEN** the app emails `you+alice@gmail.com`, which is not on the allow-list
- **THEN** nothing is sent, and the log shows the subject and the recipient as `y**+alice@g****.com`, without the email's body or any link

#### Scenario: Missing allow-list sends nothing
- **WHEN** a test environment starts without an allow-list and the app tries to send an email
- **THEN** nothing is sent and the drop is logged

#### Scenario: Production unchanged
- **WHEN** the app runs without an environment label
- **THEN** every email is sent to its recipient without a subject prefix

### Requirement: Test environments are visibly marked
When the app runs with an environment label, every page SHALL show a banner with that label that cannot be dismissed, and every response SHALL tell search engines not to index it. Without a label, nothing extra SHALL be shown or sent.

#### Scenario: Banner on staging
- **WHEN** someone opens any page on staging, signed in or not
- **THEN** a "TEST" banner is visible at the top

#### Scenario: Not indexed
- **WHEN** a search engine requests any staging page
- **THEN** the response carries `X-Robots-Tag: noindex`

#### Scenario: Production has no banner
- **WHEN** someone opens any production page
- **THEN** no banner is shown and no `noindex` header is sent

### Requirement: Staging runs the reservation job
Staging SHALL have its own monthly reservation job, with the same schedule and image as its app, using staging's database and its email allow-list. It SHALL also be possible to start it by hand.

#### Scenario: Reminder on staging
- **WHEN** the staging reservation job runs and a seeded reservation needs a reminder
- **THEN** the reminder is sent only if its recipient is on the allow-list, with a `[TEST] ` subject

### Requirement: Staging cost is capped
Staging's resource group SHALL have its own monthly budget that emails the owner at the usual thresholds and stops the staging app when it is used up.

#### Scenario: Staging budget runs out
- **WHEN** staging's actual spend reaches its budget
- **THEN** the owner is emailed and the staging app is stopped, while production keeps running
