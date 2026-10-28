## Context

Postgres Flexible Server (PG 17) has one admin, `wishlistadmin`. The app and jobs use it through the `db-connection` Container Apps secret, which Bicep builds. The deploy workflow opens a temporary firewall rule for the runner and runs the EF migrations bundle as the admin, so the admin owns every table.

## Decisions

**1. A password login made by CI, not by a migration.**
Logins exist server-wide and need a password, which shouldn't live in the repository or the image. A migration would also run in every developer and test database. A SQL script run by the deploy workflow keeps it in production, where the secret is. *Alternative:* Entra ID (managed identity) sign-in, which has no password at all. The user chose the simpler password login.

**2. Rights: rows and sequences only; default privileges for future tables.**
`GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES` plus `USAGE, SELECT ON ALL SEQUENCES`, then `ALTER DEFAULT PRIVILEGES` for the admin (the role running the script and the migrations). `__EFMigrationsHistory` stays read-only. Connecting and using schema `public` come from the PUBLIC defaults. Creating objects in `public` isn't allowed for PUBLIC since PG 15. The script re-grants on every deploy, which also repairs anything missed.

**3. Password through a psql variable on stdin.**
`echo "ALTER ROLE wishlist_app PASSWORD :'app_password';" | psql -v app_password=…`. psql quotes it safely, and the value isn't part of the SQL text in the workflow file. The step fails when the secret is missing, so a deploy can't silently leave the login without a password.

**4. Order: deploy first, then infra.**
The connection string switches only when **infra** runs. The login must exist before that, so the first deploy after merging creates it, and then infra is run. Rotating the app password works the same way, with a short gap in between (documented).

## Risks / Trade-offs

- [The admin can't run part of the script on Azure] → the script only uses `CREATE ROLE` (the admin has `CREATEROLE`), and grants on tables it owns. A failure stops the deploy before the new image goes out, and the app keeps running on the admin login.
- [`psql` on the runner] → it's preinstalled on GitHub's Ubuntu images. `sslmode=verify-full` with the system CA store, like the app.
- [A future feature needs more than rows (e.g. `TRUNCATE`, temp tables)] → it fails in the Postgres test that runs the app as `wishlist_app`, then the script is extended.
