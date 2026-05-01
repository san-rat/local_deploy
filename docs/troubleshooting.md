# Troubleshooting

## Port 80 Is Already In Use

Find the process using port `80`:

```bash
sudo lsof -i :80
```

Stop that process, or temporarily change the published Nginx port in `docker-compose.yml`.

## Docker Permission Denied

Add your user to the Docker group:

```bash
sudo usermod -aG docker $USER
```

Log out and log back in, then verify:

```bash
docker ps
```

## Stack Does Not Start Cleanly

Check service status:

```bash
docker compose ps
```

Check logs:

```bash
docker compose logs
docker compose logs backend
docker compose logs database
docker compose logs nginx
```

## Backend Cannot Connect To Database

Confirm the backend uses the Compose service name:

```text
DB_HOST=database
```

Then check:

```bash
docker compose ps
docker compose logs database
docker compose logs backend
```

## Database Data Did Not Reset

The PostgreSQL volume persists across normal shutdowns. To reset everything:

```bash
docker compose down -v
docker compose up --build
```

This deletes the local database volume.

## Backup Or Restore Fails

Make sure the stack is running:

```bash
docker compose up -d --build
docker compose ps
```

If restore says the file is missing, check the path:

```bash
ls backups/
```

If restore fails while loading SQL, check database logs:

```bash
docker compose logs database
```

If restore verification fails, run the pieces separately:

```bash
./scripts/backup-db.sh
./scripts/restore-db.sh backups/localdeploydb_YYYYMMDD_HHMMSS.sql
```

The verification script intentionally inserts a temporary task and expects restore to remove it. If the script is interrupted after insertion, rerun `./scripts/verify-restore.sh` or restore the latest backup manually.

## Backup Cleanup Does Not Remove Files

The cleanup script only removes SQL files in `backups/`:

```bash
./scripts/cleanup-backups.sh
```

Use a positive integer to change the keep count:

```bash
./scripts/cleanup-backups.sh 14
```

Non-SQL files are left alone.

## Frontend Cannot Call API

For the Dockerized stack, use:

```text
http://localhost
```

The frontend should call relative paths like `/api/tasks`. Nginx proxies those requests to the backend.

## Prometheus Or Grafana Does Not Start

Monitoring services run only when the profile is enabled:

```bash
docker compose --profile monitoring up -d --build
docker compose --profile monitoring ps
```

Check logs:

```bash
docker compose logs prometheus
docker compose logs grafana
```

If ports are already in use, change `PROMETHEUS_PORT` or `GRAFANA_PORT` in `.env`.

## Prometheus Targets Are Down

Open:

```text
http://localhost:9090/targets
```

Confirm the app services are healthy:

```bash
docker compose ps
curl http://localhost/health
curl http://localhost/api/activity
```

The `/metrics` endpoints are internal only. Use Prometheus targets or run a curl from inside the Docker network instead of trying `http://localhost/metrics`.

## Grafana Dashboard Is Empty

Generate a little traffic, then wait for the next Prometheus scrape:

```bash
curl http://localhost/api/tasks
curl http://localhost/api/tasks/summary
curl http://localhost/api/activity
```

The default dashboard uses a short time range. Make sure the time picker includes the last few minutes.

## k6 Cannot Reach The App

Make sure the stack is healthy:

```bash
docker compose up -d --build
curl http://localhost/health
```

Run k6 with the host gateway mapping:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway grafana/k6 run - < tests/load/localdeploy-read-load.js
```

If that still cannot connect, use the Docker bridge gateway:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway \
  -e BASE_URL=http://172.17.0.1 \
  grafana/k6 run - < tests/load/localdeploy-read-load.js
```

## GitHub Actions Fails

The workflow runs backend tests, Compose validation, and Docker image builds. Common checks:

- Confirm `dotnet test backend/LocalDeploy.Api.Tests/LocalDeploy.Api.Tests.csproj --configuration Release` passes locally.
- Confirm `bash -n scripts/verify-restore.sh` and `bash -n scripts/cleanup-backups.sh` pass locally.
- Confirm `docker compose config` passes locally.
- Confirm `docker compose --profile monitoring config` passes locally.
- Confirm Dockerfiles build locally with `docker compose build`.
