## MODIFIED Requirements

### Requirement: Production deploys are gated and migrate the schema first

The system SHALL deploy to production only from the `main` branch through CI, only after the same build has been deployed to staging and passed its smoke check, and only after a human approves the deployment. It SHALL apply pending EF Core migrations to the production database before switching traffic to the new image. Images SHALL be referenced by an immutable commit-derived tag.

#### Scenario: Deploy waits for approval

- **WHEN** a commit lands on `main` and the image build succeeds
- **THEN** the production deploy job does not run until an approver approves it in the `production` GitHub environment

#### Scenario: Deploy waits for staging

- **WHEN** a commit lands on `main` and the image build succeeds
- **THEN** the production deploy job does not start until that build's staging deploy and smoke check have succeeded

#### Scenario: Failed staging deploy blocks production

- **WHEN** the staging deploy or its smoke check fails for a build
- **THEN** that build is not deployed to production

#### Scenario: Migrations run before the new image takes traffic

- **WHEN** an approved deploy contains a new migration
- **THEN** the migration is applied to the production database and only afterwards is the container app updated to the new `sha-` image tag

#### Scenario: Failed migration aborts the deploy

- **WHEN** applying migrations fails
- **THEN** the container app keeps running the previous image and the workflow run fails

#### Scenario: Temporary database access is always revoked

- **WHEN** the deploy job finishes, whether successfully or not
- **THEN** the firewall rule it opened for the CI runner is removed

### Requirement: CI authenticates to Azure without stored credentials

CI SHALL authenticate to Azure using OpenID Connect federated credentials, one identity per GitHub environment (`production` and `staging`), each able to change only its own environment's resource group. CI SHALL NOT store any Azure client secret or password in GitHub.

#### Scenario: Workflow outside a deploy environment cannot deploy

- **WHEN** a workflow job that is not bound to the `production` or `staging` environment attempts to log in to Azure
- **THEN** the token exchange is rejected and no Azure resource can be changed

#### Scenario: Staging identity cannot change production

- **WHEN** a job in the `staging` environment tries to change a resource in the production resource group
- **THEN** Azure refuses it

### Requirement: The app uses a least-privilege database login
In deployed environments the app and its jobs SHALL connect to the database as `wishlist_app`, a login that can only read, insert, update and delete rows and use sequences. That login SHALL NOT be able to create, alter or drop database objects, or to change the migration history. Migrations SHALL keep running from CI as the database's admin login (the Azure server admin in production, the external database's owner on staging). Every deploy SHALL, after applying migrations, grant `wishlist_app` these rights on all tables (including new ones) and set its password from a platform secret.

#### Scenario: Normal use works with row rights
- **WHEN** the app starts and people sign up, sign in, create circles and add wishes
- **THEN** everything works while connected as `wishlist_app`

#### Scenario: Schema changes are refused
- **WHEN** a statement sent as `wishlist_app` tries to drop, alter or create a table, or to change `__EFMigrationsHistory`
- **THEN** the database refuses it with insufficient privilege

#### Scenario: New tables are covered
- **WHEN** a deploy applies a migration that adds a table
- **THEN** `wishlist_app` can read and write that table before the new image starts

#### Scenario: Missing password secret stops the deploy
- **WHEN** the deploy runs without the app login's password secret
- **THEN** it fails before deploying the new image
