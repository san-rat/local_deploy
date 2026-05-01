#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKUP_DIR="$PROJECT_ROOT/backups"
KEEP_COUNT="${1:-7}"

usage() {
    echo "Usage: ./scripts/cleanup-backups.sh [keep-count]" >&2
}

if [ "$#" -gt 1 ]; then
    usage
    exit 1
fi

if ! [[ "$KEEP_COUNT" =~ ^[0-9]+$ ]] || [ "$KEEP_COUNT" -lt 1 ]; then
    echo "keep-count must be a positive integer." >&2
    usage
    exit 1
fi

if [ ! -d "$BACKUP_DIR" ]; then
    echo "No backups directory found. Nothing to clean up."
    exit 0
fi

mapfile -t OLD_BACKUPS < <(
    find "$BACKUP_DIR" -maxdepth 1 -type f -name "*.sql" -printf '%T@ %p\n' \
        | sort -nr \
        | awk -v keep="$KEEP_COUNT" 'NR > keep {print $2}'
)

if [ "${#OLD_BACKUPS[@]}" -eq 0 ]; then
    echo "No old backup files to remove. Keeping newest $KEEP_COUNT backup(s)."
    exit 0
fi

for backup_file in "${OLD_BACKUPS[@]}"; do
    rm -f "$backup_file"
    echo "Removed old backup: ${backup_file#$PROJECT_ROOT/}"
done

echo "Backup cleanup completed. Kept newest $KEEP_COUNT backup(s)."
