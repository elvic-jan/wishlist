## Why

The app and its jobs connect to production Postgres as the server admin. The admin can drop tables, change the schema, and create logins. A bug, a SQL injection or a leaked connection string therefore puts the whole database at risk, not just its rows. The app only ever reads and writes rows; schema changes come from migrations, which CI runs separately.

## What Changes

- **New login `wishlist_app`** that can only `SELECT`, `INSERT`, `UPDATE` and `DELETE` rows and use sequences. It can't create, alter or drop anything, and it can't touch the migration history.
- `infra/sql/app-role.sql` creates the login and grants the rights. It also sets default privileges, so tables from future migrations are covered. The deploy workflow runs it after the migrations on every deploy, then sets the login's password from the new environment secret `POSTGRES_APP_PASSWORD`.
- The app and jobs' connection string (Bicep, `db-connection` secret) uses `wishlist_app` instead of the admin. The admin password is then used only by CI for migrations.
- No app code changes, and nothing changes in the app for users, so there's no update notice.
- **Not changing:** the "Allow Azure services" firewall rule stays (user's decision on 2026-10-28). Closing it needs a private network, at ~€7–8/month plus a new Container Apps environment.

## Rollout (one time)

1. Add the secret `POSTGRES_APP_PASSWORD` to the `production` environment.
2. Merge. The deploy creates `wishlist_app` and sets its password, while the app keeps using the admin.
3. Run the **infra** workflow. The connection string switches to `wishlist_app`, and the new revision starts with it.

## Capabilities

### Modified Capabilities
- `hosting`: the app connects with a least-privilege database login. Deploys keep its rights in step with the schema.

## Impact

- **Infra**: `infra/sql/app-role.sql`; `main.bicep` and `containerapp.bicep` get the `postgresAppPassword` param, and the connection string uses `wishlist_app`; `infra.yml` passes the secret; `deploy.yml` checks out `infra/sql` and runs the script with `psql`; README.
- **Tests**: a Postgres test runs the script twice, then runs the app as `wishlist_app` (startup key storage, sign-up, circle, item, token rotation and cleanup) and checks that schema changes are refused.
