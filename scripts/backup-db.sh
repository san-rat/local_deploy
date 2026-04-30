#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

if [ -f ".env" ]; then
    set -a
    # shellcheck disable=SC1091
    source ".env"
    set +a
fi

POSTGRES_DB="${POSTGRES_DB:-localdeploydb}"
POSTGRES_USER="${POSTGRES_USER:-localdeploy_user}"
BACKUP_DIR="$PROJECT_ROOT/backups"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP_FILE="$BACKUP_DIR/${POSTGRES_DB}_${TIMESTAMP}.sql"
TEMP_FILE="${BACKUP_FILE}.tmp"

mkdir -p "$BACKUP_DIR"

echo "Checking database container..."
docker compose exec -T database pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" > /dev/null

echo "Creating backup..."
if docker compose exec -T database pg_dump \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    --no-owner \
    --no-privileges \
    > "$TEMP_FILE"; then
    mv "$TEMP_FILE" "$BACKUP_FILE"
    echo "Backup created: ${BACKUP_FILE#$PROJECT_ROOT/}"
else
    rm -f "$TEMP_FILE"
    echo "Backup failed." >&2
    exit 1
fi
