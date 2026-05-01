# Setup Guide

This guide explains how to run LocalDeploy Lab from a fresh clone on Ubuntu Linux.

## Prerequisites

- Docker Engine
- Docker Compose plugin
- Git

Optional local development tools:

- .NET 8 SDK for backend development and tests
- Node.js and npm for frontend development
- Docker can run the k6 load test without a host k6 install

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
| `REDIS_HOST` | `redis` |
| `REDIS_PORT` | `6379` |
| `REDIS_CACHE_SECONDS` | `60` |
| `ACTIVITY_SERVICE_URL` | `http://activity-service:8081` |
| `ENABLE_SWAGGER` | `true` in dev, `false` in production-style mode |
| `NGINX_HTTP_PORT` | `80` |
| `PROMETHEUS_PORT` | `9090` |
| `GRAFANA_PORT` | `3000` |
| `GRAFANA_ADMIN_USER` | `admin` |
| `GRAFANA_ADMIN_PASSWORD` | `localdeploy_admin` |

Change password values in `.env` before using the production-style stack in any shared or deployed environment.

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
curl http://localhost/api/tasks/summary
curl http://localhost/api/activity
curl http://localhost/swagger/v1/swagger.json
```

## Run With Monitoring

Stage V4 adds Prometheus and Grafana as optional services behind the `monitoring` Compose profile. The normal app does not need them.

Start the stack with monitoring:

```bash
docker compose --profile monitoring up -d --build
```

Open:

```text
http://localhost:9090
http://localhost:3000
```

Grafana signs in with `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD`. The Prometheus datasource and `LocalDeploy Platform` dashboard are provisioned automatically.

Useful checks:

```bash
curl http://localhost:9090/-/ready
curl http://localhost:3000/api/health
```

On an 8GB machine, stop Prometheus and Grafana after testing or taking screenshots:

```bash
docker compose --profile monitoring down
```

## Run The Production-Style Stack

Stage V1 includes a production-style Compose override. It requires env values, adds restart policies, keeps only Nginx public, disables Swagger by default, uses production Nginx security headers, and stores data in a separate `postgres_prod_data` volume. Redis remains internal and cache-only in both development and production-style runs.

Prepare the env file:

```bash
cp .env.example .env
```

Start the production-style stack:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Useful verification commands:

```bash
curl http://localhost/health
curl http://localhost/api/tasks/summary
curl http://localhost/api/activity
curl -I http://localhost
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml ps
```

Swagger is disabled by default in this mode. Set `ENABLE_SWAGGER=true` in `.env` only when you intentionally need Swagger for a controlled demo.

## Deploy To Azure

Stage V6 uses an Azure for Students subscription, one Ubuntu VM, and the existing production Compose override.

Use the portal-first guide:

```text
docs/deployment-guide.md
```

Keep cost notes nearby:

```text
docs/cloud-cost-notes.md
```

Cloud deployment command on the VM:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up -d --build
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

For the production-style stack, use the same `-f` flags when stopping or deleting its separate production volume:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml down
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml down -v
```

If you started the monitoring profile, include the same profile when stopping it:

```bash
docker compose --profile monitoring down
```

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

Verify backup and restore end to end:

```bash
./scripts/verify-restore.sh
```

Clean up old backups while keeping the newest 7:

```bash
./scripts/cleanup-backups.sh
```

Keep a custom number of backups:

```bash
./scripts/cleanup-backups.sh 14
```

See [Backup And Recovery](backup-and-recovery.md) for details.

## Load Testing

Run the laptop-safe k6 read test:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway grafana/k6 run - < tests/load/localdeploy-read-load.js
```

If `host.docker.internal` does not work on your Linux Docker setup, use:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway \
  -e BASE_URL=http://172.17.0.1 \
  grafana/k6 run - < tests/load/localdeploy-read-load.js
```

Record results in [Performance Results](performance-results.md).

## Redis Cache

Redis caches dashboard summary counts for `60` seconds by default. The backend deletes the summary cache after successful task create, update, or delete operations.

Redis is cache-only. Stopping or recreating the Redis container does not delete source task data because PostgreSQL remains the system of record.

Check Redis fallback behavior:

```bash
docker compose stop redis
curl http://localhost/health
curl http://localhost/api/tasks/summary
docker compose start redis
```

## Activity Service

The activity service records task-created, task-updated, and task-deleted events in PostgreSQL. The task API sends events with best-effort internal HTTP calls, so task operations keep working if the activity service is temporarily unavailable.

Check recent events:

```bash
curl http://localhost/api/activity
```

Check resilience:

```bash
docker compose stop activity-service
curl -X POST http://localhost/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Activity fallback test","priority":"Low"}'
docker compose start activity-service
```

## Monitoring Screenshots

Suggested portfolio screenshot targets:

- `docs/screenshots/grafana-dashboard.png`
- `docs/screenshots/prometheus-targets.png`
- `docs/screenshots/restore-verification.png`
- `docs/screenshots/k6-load-test.png`
