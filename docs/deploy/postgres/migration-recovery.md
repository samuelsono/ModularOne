# Database reset and migration recovery

## Why `relation "AspNetRoles" already exists` happens

Host Identity migrations start with `InitialAuth`, which creates `AspNetRoles` before `AspNetUsers`. Startup migration uses probe tables and `__EFMigrationsHistory`:

| State | Behaviour |
|-------|-----------|
| Empty `public` schema | Run all Host migrations |
| Both `AspNetUsers` and `AspNetRoles` exist, history empty or missing `InitialAuth` | Baseline (record migrations, do not re-run CREATE) |
| Only some Identity tables exist, history empty | Fail with a clear partial-schema error |
| Concurrent app instances migrating | Serialized with a Postgres advisory lock |

A common **live** failure mode (user data present):

1. Identity / module tables already exist from an earlier deploy.
2. `__EFMigrationsHistory` is missing, empty, or missing early IDs after modularization / a history rewrite.
3. Host still treats `InitialAuth` as pending and runs `CREATE TABLE "AspNetRoles"` → `42P07`.

**Do not wipe `public` on a database with user data.** Use the live baseline below, then deploy the build that baselines automatically.

Deleting migration C# files and adding a new Initial migration does **not** fix a dirty database. History IDs and tables on the server must still match.

## Live database (preserve user data) — baseline Host history

Use this when tables like `AspNetRoles` / `AspNetUsers` already exist and the app crashes with `42P07`.

1. Stop the app:

```bash
sudo systemctl stop chronos
```

2. Inspect what exists:

```bash
sudo -u postgres psql -d cartrack <<'SQL'
SELECT tablename FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN ('AspNetRoles', 'AspNetUsers', '__EFMigrationsHistory')
ORDER BY 1;

SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;
SQL
```

(If the history table is missing, the second query errors — that is expected.)

3. Ensure the history table exists, then insert **every Host migration id from the deployed assembly** that is not already recorded. Easiest path after deploying a fixed build: restart once and let `MigrateModuleAsync` baseline. If you must unblock an older build immediately, insert at least through the migrations that created the current Identity schema, for example:

```bash
sudo -u postgres psql -d cartrack <<'SQL'
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
  "MigrationId" character varying(150) NOT NULL,
  "ProductVersion" character varying(32) NOT NULL,
  CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Minimal unblock: mark InitialAuth applied so CREATE AspNetRoles is skipped.
-- Prefer restarting a build that baselines the full pending prefix automatically.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260708144317_InitialAuth', '10.0.8')
ON CONFLICT ("MigrationId") DO NOTHING;
SQL
```

4. Start the app again:

```bash
sudo systemctl start chronos
sudo journalctl -u chronos -e --no-pager | tail -80
```

If a later Host migration still fails because its objects already exist, insert that migration id the same way (or deploy the auto-baseline build). **Never** `DROP SCHEMA public` on this database.

## Clean wipe (only for empty / disposable environments)

As a superuser / `postgres` OS user, against the app database (replace `cartrack` / role as needed):

```bash
sudo -u postgres psql -d cartrack <<'SQL'
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO cartrack;
GRANT ALL ON SCHEMA public TO public;
ALTER SCHEMA public OWNER TO cartrack;
SQL
```

Then restart the app once:

```bash
sudo systemctl restart chronos
```

Confirm history tables were created:

```bash
sudo -u postgres psql -d cartrack -c '\dt'
sudo -u postgres psql -d cartrack -c 'SELECT * FROM "__EFMigrationsHistory";'
sudo -u postgres psql -d cartrack -c 'SELECT * FROM "__EFMigrationsHistory_Users";'
```

## Do not

- Drop only `__EFMigrationsHistory*` while leaving `AspNet*` / module tables (unless you intentionally re-baseline immediately).
- `DROP SCHEMA public CASCADE` on production with user data.
- Point `ConnectionStrings__cartrack` at a different database that still has Identity tables.
- Run two app instances against an empty DB without the advisory-lock build (older builds can race).

## After squashing migrations locally

If you replace the Host migration chain with a single new Initial:

1. Wipe `public` as above on every **empty** environment that will use the new assembly, **or**
2. Keep the old migration IDs in history and only add forward migrations, **or**
3. On live DBs, baseline the new IDs without re-running CREATE (auto-baseline when probe tables exist).

Never deploy a rewritten Initial onto a database that already has tables unless you intentionally baseline or wipe.
