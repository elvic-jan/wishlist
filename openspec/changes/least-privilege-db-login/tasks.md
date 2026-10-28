## 1. Database login

- [x] 1.1 `infra/sql/app-role.sql`: create `wishlist_app` if missing; row and sequence rights on all tables; default privileges for future tables; migration history read-only
- [x] 1.2 `deploy.yml`: check out `infra/sql` (before the artifact download), run the script after migrations, set the password from `POSTGRES_APP_PASSWORD` via a psql variable; fail when the secret is missing

## 2. Configuration

- [x] 2.1 Bicep: `postgresAppPassword` param; connection string uses `wishlist_app`
- [x] 2.2 `infra.yml` passes `POSTGRES_APP_PASSWORD`; README (secret setup, logins, rotation)

## 3. Tests

- [x] 3.1 Postgres: script runs twice; the app works as `wishlist_app`; schema changes and migration-history writes are refused

## 4. Rollout

- [ ] 4.1 User adds `POSTGRES_APP_PASSWORD` to the production environment
- [ ] 4.2 Merge; the deploy creates the login
- [ ] 4.3 Run **infra**; check the app and the reservation job against production
