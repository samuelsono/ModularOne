# PostgreSQL setup (Ubuntu)

Create a dedicated role and database for CarTrack. Run as the `postgres` OS user.

```bash
sudo -u postgres psql <<'SQL'
CREATE USER cartrack WITH PASSWORD 'CHANGE_ME_DB_PASSWORD';
CREATE DATABASE cartrack OWNER cartrack;
GRANT ALL PRIVILEGES ON DATABASE cartrack TO cartrack;
\c cartrack
GRANT ALL ON SCHEMA public TO cartrack;
ALTER SCHEMA public OWNER TO cartrack;
SQL
```

## Local-only listening (recommended for single-host)

In `/etc/postgresql/*/main/postgresql.conf`:

```conf
listen_addresses = 'localhost'
```

In `/etc/postgresql/*/main/pg_hba.conf`, prefer scram for local TCP:

```conf
# TYPE  DATABASE  USER      ADDRESS       METHOD
local   all       postgres                peer
local   cartrack  cartrack                scram-sha-256
host    cartrack  cartrack  127.0.0.1/32  scram-sha-256
host    cartrack  cartrack  ::1/128       scram-sha-256
```

```bash
sudo systemctl restart postgresql
```

## Connection string

Match `/etc/cartrack/config.env`:

```text
ConnectionStrings__cartrack=Host=127.0.0.1;Port=5432;Database=cartrack;Username=cartrack;Password=CHANGE_ME_DB_PASSWORD
```

Module migrations apply on host startup (`MigrateModuleAsync` per module). Ensure the `cartrack` role can create tables in `public`.

## Backups

```bash
sudo -u postgres pg_dump -Fc cartrack > "/var/backups/cartrack-$(date +%F).dump"
```
