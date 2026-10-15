#!/usr/bin/env sh
set -eu

umask 077

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
BACKUP_ROOT="${1:-backups}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_DIR="${BACKUP_ROOT%/}/campuscore-${TIMESTAMP}"
DB_TMP="/tmp/campuscore-${TIMESTAMP}.dump"
UPLOADS_TMP="/tmp/campuscore-${TIMESTAMP}-uploads.tar"

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

cleanup() {
  compose exec -T postgres rm -f "$DB_TMP" >/dev/null 2>&1 || true
  compose exec -T api rm -f "$UPLOADS_TMP" >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

if ! docker compose version >/dev/null 2>&1; then
  echo "Docker Compose v2 is required." >&2
  exit 1
fi

mkdir -p "$BACKUP_DIR"

echo "Creating PostgreSQL backup..."
compose exec -T postgres sh -ec \
  'pg_dump --format=custom --compress=9 --no-owner --no-acl --username "$POSTGRES_USER" "$POSTGRES_DB" > "$1"' \
  sh "$DB_TMP"
compose cp "postgres:${DB_TMP}" "$BACKUP_DIR/database.dump" >/dev/null

echo "Creating attachment backup..."
compose exec -T api sh -ec 'tar -C /data/uploads -cf "$1" .' sh "$UPLOADS_TMP"
compose cp "api:${UPLOADS_TMP}" "$BACKUP_DIR/uploads.tar" >/dev/null

(
  cd "$BACKUP_DIR"
  sha256sum database.dump uploads.tar > SHA256SUMS
)

cat > "$BACKUP_DIR/manifest.txt" <<EOF
format=campuscore-backup-v1
created_at_utc=${TIMESTAMP}
database_file=database.dump
uploads_file=uploads.tar
checksums_file=SHA256SUMS
EOF

chmod 600 "$BACKUP_DIR/database.dump" "$BACKUP_DIR/uploads.tar" "$BACKUP_DIR/SHA256SUMS" "$BACKUP_DIR/manifest.txt" 2>/dev/null || true

echo "Backup created: $BACKUP_DIR"
