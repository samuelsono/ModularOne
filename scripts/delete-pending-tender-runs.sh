#!/usr/bin/env bash

set -euo pipefail

# Defaults to the requested local Postgres endpoint using the CarTrack DB.
DB_URI="${DATABASE_URL:-postgresql://postgres@localhost:55087/cartrack}"
STATUSES="Queued,Running"
APPLY=false

usage() {
  cat <<'EOF'
Delete pending Tender scrape runs.

Usage:
  ./scripts/delete-pending-tender-runs.sh [--apply] [--db-uri URI] [--statuses CSV]

Options:
  --apply           Execute delete. Without this flag, script does dry-run only.
  --db-uri URI      PostgreSQL connection URI.
                    Default: postgresql://postgres@localhost:55087/cartrack
  --statuses CSV    Comma-separated run statuses to delete.
                    Default: Queued,Running

Examples:
  ./scripts/delete-pending-tender-runs.sh
  ./scripts/delete-pending-tender-runs.sh --apply
  ./scripts/delete-pending-tender-runs.sh --apply --statuses Queued
  DATABASE_URL='postgresql://postgres:secret@localhost:55087/postgres' ./scripts/delete-pending-tender-runs.sh --apply
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --apply)
      APPLY=true
      shift
      ;;
    --db-uri)
      DB_URI="${2:-}"
      shift 2
      ;;
    --statuses)
      STATUSES="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if ! command -v psql >/dev/null 2>&1; then
  echo "Error: psql is required but not found in PATH." >&2
  exit 1
fi

if [[ -z "$DB_URI" ]]; then
  echo "Error: --db-uri cannot be empty." >&2
  exit 1
fi

if [[ -z "$STATUSES" ]]; then
  echo "Error: --statuses cannot be empty." >&2
  exit 1
fi

# Basic guard to avoid SQL injection through status CSV.
if [[ ! "$STATUSES" =~ ^[A-Za-z,]+$ ]]; then
  echo "Error: --statuses must only contain letters and commas." >&2
  exit 1
fi

echo "Database URI: $DB_URI"
echo "Target statuses: $STATUSES"
echo

psql "$DB_URI" -v ON_ERROR_STOP=1 -v statuses="$STATUSES" <<'SQL'
SELECT current_database() AS database_name, current_schema() AS schema_name;

SELECT to_regclass('public."TenderScrapeRuns"') AS tender_runs_table;

SELECT COUNT(*) AS pending_runs
FROM "TenderScrapeRuns"
WHERE "Status" = ANY (string_to_array(:'statuses', ','));

SELECT "Id", "Status", "CreatedAt"
FROM "TenderScrapeRuns"
WHERE "Status" = ANY (string_to_array(:'statuses', ','))
ORDER BY "CreatedAt" DESC
LIMIT 20;
SQL

if [[ "$APPLY" != true ]]; then
  echo
  echo "Dry-run only. Re-run with --apply to delete matching rows."
  exit 0
fi

echo
echo "Deleting pending Tender runs..."

psql "$DB_URI" -v ON_ERROR_STOP=1 -v statuses="$STATUSES" <<'SQL'
BEGIN;

WITH deleted AS (
  DELETE FROM "TenderScrapeRuns"
  WHERE "Status" = ANY (string_to_array(:'statuses', ','))
  RETURNING "Id", "Status"
)
SELECT COUNT(*) AS deleted_runs FROM deleted;

COMMIT;
SQL

echo "Done."