#!/usr/bin/env sh
set -eu

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
BACKUP_DIR="${1:-}"

if [ -z "$BACKUP_DIR" ]; then
  echo "Usage: $0 <backup-directory>" >&2
  exit 64
fi

for file in manifest.txt SHA256SUMS database.dump uploads.tar; do
  if [ ! -f "$BACKUP_DIR/$file" ]; then
    echo "Backup is incomplete: missing $file" >&2
    exit 1
  fi
done

if ! grep -qx 'format=campuscore-backup-v1' "$BACKUP_DIR/manifest.txt"; then
  echo "Unsupported or invalid backup manifest." >&2
  exit 1
fi

(
  cd "$BACKUP_DIR"
  sha256sum -c SHA256SUMS
)

DB_TMP="/tmp/campuscore-verify-$$.dump"
UPLOADS_TMP="/tmp/campuscore-verify-$$-uploads.tar"

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

cleanup() {
  compose exec -T postgres rm -f "$DB_TMP" >/dev/null 2>&1 || true
  compose exec -T api rm -f "$UPLOADS_TMP" >/dev/null 2>&1 || true
}
trap cleanup EXIT INT TERM

compose cp "$BACKUP_DIR/database.dump" "postgres:${DB_TMP}" >/dev/null
compose exec -T postgres pg_restore --list "$DB_TMP" >/dev/null

compose cp "$BACKUP_DIR/uploads.tar" "api:${UPLOADS_TMP}" >/dev/null
compose exec -T api tar -tf "$UPLOADS_TMP" >/dev/null

echo "Backup verification passed: $BACKUP_DIR"
