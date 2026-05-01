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
VERIFY_TITLE="restore-verification-$(date +%Y%m%d%H%M%S)"

latest_backup() {
    find "$BACKUP_DIR" -maxdepth 1 -type f -name "${POSTGRES_DB}_*.sql" -printf '%T@ %p\n' 2> /dev/null \
        | sort -nr \
        | awk 'NR == 1 {print $2}'
}

count_verification_tasks() {
    docker compose exec -T database psql \
        -U "$POSTGRES_USER" \
        -d "$POSTGRES_DB" \
        -v ON_ERROR_STOP=1 \
        -v verify_title="$VERIFY_TITLE" \
        -tA <<'SQL' | tr -d '[:space:]'
SELECT COUNT(*) FROM tasks WHERE title = :'verify_title';
SQL
}

echo "Checking database container..."
docker compose exec -T database pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" > /dev/null

echo "Creating restore verification backup..."
"$SCRIPT_DIR/backup-db.sh"

BACKUP_FILE="$(latest_backup)"

if [ -z "$BACKUP_FILE" ]; then
    echo "Could not find a backup file for database '$POSTGRES_DB'." >&2
    exit 1
fi

echo "Using backup: ${BACKUP_FILE#$PROJECT_ROOT/}"

echo "Inserting temporary verification task..."
docker compose exec -T database psql \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    -v ON_ERROR_STOP=1 \
    -v verify_title="$VERIFY_TITLE" <<'SQL' > /dev/null
INSERT INTO tasks (title, description, status, priority)
VALUES (:'verify_title', 'Temporary row used by restore verification.', 'Pending', 'Low');
SQL

TEMP_COUNT="$(count_verification_tasks)"

if [ "$TEMP_COUNT" != "1" ]; then
    echo "Expected one temporary verification task, found $TEMP_COUNT." >&2
    exit 1
fi

echo "Temporary task inserted."
echo "Restoring backup..."
"$SCRIPT_DIR/restore-db.sh" "$BACKUP_FILE"

RESTORED_COUNT="$(count_verification_tasks)"

if [ "$RESTORED_COUNT" != "0" ]; then
    echo "Restore verification failed. Temporary task still exists after restore." >&2
    exit 1
fi

echo "Restore verification passed. Temporary task was removed by restore."
