# Setup Guide

This guide explains how to run LocalDeploy Lab from a fresh clone on Ubuntu Linux.

## Prerequisites

- Docker Engine
- Docker Compose plugin
- Git

Optional local development tools:

- .NET 8 SDK for backend development and tests
- Node.js and npm for frontend development

## Environment

The project works with built-in defaults. To customize local values, copy the example file:

```bash
cp .env.example .env
```

Important defaults:

| Variable | Default |
| --- | --- |
| `POSTGRES_DB` | `localdeploydb` |
| `POSTGRES_USER` | `localdeploy_user` |
| `POSTGRES_PASSWORD` | `localdeploy_password` |
| `DB_HOST` | `database` |
| `DB_PORT` | `5432` |

## Run The Stack

Start the full Dockerized application:

```bash
docker compose up --build
```

Open the dashboard:

```text
http://localhost
```

Useful verification commands:

```bash
curl http://localhost/health
curl http://localhost/api/tasks
curl http://localhost/swagger/v1/swagger.json
```

## Stop Or Reset

Stop containers while keeping database data:

```bash
docker compose down
```

Stop containers and delete database data:

```bash
docker compose down -v
```

Use `down -v` carefully because it removes the PostgreSQL volume.

## Local Backend Tests

Run backend validation tests:

```bash
dotnet test backend/LocalDeploy.Api.Tests/LocalDeploy.Api.Tests.csproj --configuration Release
```

## Database Backup And Restore

Create a backup:

```bash
./scripts/backup-db.sh
```

Restore a backup:

```bash
./scripts/restore-db.sh backups/localdeploydb_YYYYMMDD_HHMMSS.sql
```

Restore resets the current `public` schema before loading the selected backup.
