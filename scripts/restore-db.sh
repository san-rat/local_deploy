#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

usage() {
    echo "Usage: ./scripts/restore-db.sh backups/<backup-file>.sql" >&2
}

if [ "$#" -ne 1 ]; then
    usage
    exit 1
fi

cd "$PROJECT_ROOT"

if [ -f ".env" ]; then
    set -a
    # shellcheck disable=SC1091
    source ".env"
    set +a
fi

POSTGRES_DB="${POSTGRES_DB:-localdeploydb}"
POSTGRES_USER="${POSTGRES_USER:-localdeploy_user}"
BACKUP_INPUT="$1"

if [[ "$BACKUP_INPUT" = /* ]]; then
    BACKUP_FILE="$BACKUP_INPUT"
else
    BACKUP_FILE="$PROJECT_ROOT/$BACKUP_INPUT"
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Backup file not found: $BACKUP_INPUT" >&2
    usage
    exit 1
fi

echo "Checking database container..."
docker compose exec -T database pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" > /dev/null

echo "Resetting public schema in database '$POSTGRES_DB'..."
docker compose exec -T database psql \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -v ON_ERROR_STOP=1 \
    -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

echo "Restoring backup: ${BACKUP_FILE#$PROJECT_ROOT/}"
docker compose exec -T database psql \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -v ON_ERROR_STOP=1 \
    < "$BACKUP_FILE"

echo "Restore completed."
