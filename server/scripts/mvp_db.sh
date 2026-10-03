#!/usr/bin/env bash
# Stand up an ISOLATED local Postgres for the MVP: its own cluster + port, not
# docker, not the VEX prod DB. Idempotent - safe to re-run. Applies migrations
# and seeds every fixture session so known student_ids resolve.
set -euo pipefail

PG=/usr/lib/postgresql/16/bin
SERVER_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PGDATA="$SERVER_DIR/.pgdata"
RUNDIR="$SERVER_DIR/.run"
SOCKDIR="$RUNDIR/pgsock"
PORT=5544
DB=vexagent
# Fixed superuser (not $USER) so a cluster copied between machines still connects.
PGUSER=postgres
export PGUSER
export DATABASE_URL="postgresql://$PGUSER@127.0.0.1:$PORT/$DB"

mkdir -p "$SOCKDIR"

if [ ! -f "$PGDATA/PG_VERSION" ]; then
  "$PG/initdb" -D "$PGDATA" -A trust -U "$PGUSER" >/dev/null
fi

if ! "$PG/pg_ctl" -D "$PGDATA" status >/dev/null 2>&1; then
  "$PG/pg_ctl" -D "$PGDATA" -l "$RUNDIR/pg.log" \
    -o "-p $PORT -k $SOCKDIR -c listen_addresses=127.0.0.1" start
  sleep 1
fi

"$PG/psql" -h 127.0.0.1 -p "$PORT" -d postgres -tc \
  "SELECT 1 FROM pg_database WHERE datname='$DB'" | grep -q 1 \
  || "$PG/createdb" -h 127.0.0.1 -p "$PORT" "$DB"

for f in "$SERVER_DIR"/db/migrations/*.sql; do
  PGOPTIONS="-c client_min_messages=warning" \
    "$PG/psql" -q -v ON_ERROR_STOP=1 -h 127.0.0.1 -p "$PORT" -d "$DB" -f "$f"
done

# Seed every fixture session (the DB-backed tests need them all; parsed_events
# dedupes on natural key, so re-runs are safe).
for f in "$SERVER_DIR"/tests/fixtures/raw_logs/*.ndjson; do
  PYTHONPATH="$SERVER_DIR" python -m vex_agent.ingest.parse_event_logs --input "$f" --insert >/dev/null
done

echo "---"
echo "DATABASE_URL=$DATABASE_URL"
echo "Seeded student_id(s):"
"$PG/psql" -h 127.0.0.1 -p "$PORT" -d "$DB" -tAc \
  "SELECT DISTINCT student_id FROM event_logs.parsed_events;"
