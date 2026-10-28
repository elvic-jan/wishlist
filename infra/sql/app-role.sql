-- The app's own database login: it can read and write rows, nothing else.
-- It cannot create, change or drop tables, so a bug or a leaked connection
-- string can't wreck the schema. Migrations keep running as the admin.
--
-- Run as the admin (the role that runs migrations and owns the tables), after
-- migrations, on every deploy. Safe to run again. The password is set
-- separately (see .github/workflows/deploy.yml).

DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'wishlist_app') THEN
    CREATE ROLE wishlist_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
  END IF;
END
$$;

-- Connecting to the database and using schema public come from PostgreSQL's
-- defaults for every role (PUBLIC); creating objects in public doesn't (15+).

-- Existing tables, and every table later migrations create (they run as the
-- same admin, so these defaults apply to them).
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO wishlist_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO wishlist_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO wishlist_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO wishlist_app;

-- The migration history is the admin's business.
REVOKE INSERT, UPDATE, DELETE ON "__EFMigrationsHistory" FROM wishlist_app;
