# Database reset and migration recovery

## Why `relation "AspNetRoles" already exists` happens

Host Identity migrations start with `InitialAuth`, which creates `AspNetRoles` before `AspNetUsers`. Startup migration uses probe tables and `__EFMigrationsHistory`:

| State | Behaviour |
|-------|-----------|
| Empty `public` schema | Run all Host migrations |
| Both `AspNetUsers` and `AspNetRoles` exist, history empty | Baseline (record migrations, do not re-run) |
| Only some Identity tables exist, history empty | Fail with a clear partial-schema error |
| Concurrent app instances migrating | Serialized with a Postgres advisory lock |

A common live failure mode:

1. Database is “recreated” but `public` still has leftover tables, **or** a previous start created `AspNetRoles` then crashed / raced.
2. `__EFMigrationsHistory` is missing or empty.
3. Probe used to look only at `AspNetUsers`, so Host tried to run `InitialAuth` again → `42P07 AspNetRoles already exists`.

Deleting migration C# files and adding a new Initial migration does **not** fix a dirty database. History IDs and tables on the server must still match.

## Clean wipe (recommended for empty-prod bootstrap)

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
sudo systemctl restart cartrack
# or: chronos
sudo systemctl restart chronos
```

Confirm history tables were created:

```bash
sudo -u postgres psql -d cartrack -c '\dt'
sudo -u postgres psql -d cartrack -c 'SELECT * FROM "__EFMigrationsHistory";'
sudo -u postgres psql -d cartrack -c 'SELECT * FROM "__EFMigrationsHistory_Users";'
```

## Do not

- Drop only `__EFMigrationsHistory*` while leaving `AspNet*` / module tables.
- Point `ConnectionStrings__cartrack` at a different database that still has Identity tables.
- Run two app instances against an empty DB without the advisory-lock build (older builds can race).

## After squashing migrations locally

If you replace the Host migration chain with a single new Initial:

1. Wipe `public` as above on every environment that will use the new assembly, **or**
2. Keep the old migration IDs in history and only add forward migrations.

Never deploy a rewritten Initial onto a database that already has tables unless you intentionally baseline or wipe.
