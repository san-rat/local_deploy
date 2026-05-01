# Backup And Recovery

LocalDeploy Lab stores durable application data in PostgreSQL. Backups are local SQL dumps written to `backups/`, which is ignored by Git.

## Create A Backup

Start the stack first:

```bash
docker compose up -d --build
```

Create a timestamped SQL backup:

```bash
./scripts/backup-db.sh
```

Backups use this shape:

```text
backups/localdeploydb_YYYYMMDD_HHMMSS.sql
```

## Restore A Backup

Restore into the running PostgreSQL container:

```bash
./scripts/restore-db.sh backups/localdeploydb_YYYYMMDD_HHMMSS.sql
```

Restore drops and recreates the `public` schema before loading the backup. This is destructive for the current local database state.

## Verify Recovery

Run the end-to-end recovery check:

```bash
./scripts/verify-restore.sh
```

The verification script:

- creates a fresh backup
- inserts a temporary verification task
- confirms that temporary task exists
- restores the backup
- confirms the temporary task no longer exists

Expected success message:

```text
Restore verification passed. Temporary task was removed by restore.
```

## Cleanup Old Backups

Keep the newest 7 SQL backups:

```bash
./scripts/cleanup-backups.sh
```

Keep a custom number:

```bash
./scripts/cleanup-backups.sh 14
```

The cleanup script only removes `backups/*.sql` files. It does not delete non-SQL files.

## Notes

These scripts are local operational evidence, not enterprise backup automation. Later stages could add scheduled backups, off-machine storage, encryption, and automated recovery checks in CI.
