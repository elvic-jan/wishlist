## ADDED Requirements

### Requirement: The app uses a least-privilege database login
In deployed environments the app and its jobs SHALL connect to the database as `wishlist_app`, a login that can only read, insert, update and delete rows and use sequences. That login SHALL NOT be able to create, alter or drop database objects, or to change the migration history. Migrations SHALL keep running as the server admin from CI. Every deploy SHALL, after applying migrations, grant `wishlist_app` these rights on all tables (including new ones) and set its password from a platform secret.

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
