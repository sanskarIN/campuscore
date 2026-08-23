#!/usr/bin/env sh
set -eu

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
BACKUP_DIR="${1:-}"
CONFIRM="${2:-}"
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"

if [ -z "$BACKUP_DIR" ] || [ "$CONFIRM" != "--confirm" ]; then
  echo "Usage: $0 <backup-directory> --confirm" >&2
  echo "Restore is destructive: the current database and attachments will be replaced." >&2
  exit 64
fi

if [ ! -d "$BACKUP_DIR" ]; then
  echo "Backup directory does not exist: $BACKUP_DIR" >&2
  exit 1
fi

BACKUP_DIR="$(CDPATH= cd -- "$BACKUP_DIR" && pwd)"
UPLOADS_FILE="$BACKUP_DIR/uploads.tar"
DB_TMP="/tmp/campuscore-restore-$$.dump"

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

cleanup() {
  compose exec -T --user root postgres rm -f "$DB_TMP" >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

"$SCRIPT_DIR/verify-backup.sh" "$BACKUP_DIR"

echo "Stopping CampusCore application services..."
compose stop web api >/dev/null

echo "Restoring PostgreSQL database..."
compose cp "$BACKUP_DIR/database.dump" "postgres:${DB_TMP}" >/dev/null
if ! compose exec -T --user root postgres sh -ec \
  'dropdb --if-exists --force --username "$POSTGRES_USER" "$POSTGRES_DB" && createdb --username "$POSTGRES_USER" "$POSTGRES_DB" && pg_restore --exit-on-error --no-owner --no-acl --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" "$1"' \
  sh "$DB_TMP"; then
  echo "Database restore failed. API and web services remain stopped for inspection." >&2
  exit 1
fi

echo "Restoring attachment volume..."
if ! compose run --rm --no-deps -T --user root \
  --volume "$UPLOADS_FILE:/tmp/campuscore-uploads.tar:ro" \
  --entrypoint sh api -ec \
  'find /data/uploads -mindepth 1 -maxdepth 1 -exec rm -rf {} + && tar -xf /tmp/campuscore-uploads.tar -C /data/uploads && chown -R campuscore:campuscore /data/uploads'; then
  echo "Attachment restore failed. API and web services remain stopped for inspection." >&2
  exit 1
fi

echo "Starting CampusCore and waiting for readiness dependencies..."
compose up -d api web

echo "Restore completed from: $BACKUP_DIR"
