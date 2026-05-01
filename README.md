# LocalDeploy Lab

A lightweight DevOps home lab that runs a React frontend, ASP.NET Core task API, activity service, PostgreSQL database, Redis cache, Nginx reverse proxy, and optional Prometheus/Grafana monitoring with Docker Compose on Ubuntu Linux.

The goal is to show a small production-style full-stack system, not only an application. The browser uses one entry point, Nginx routes API traffic internally, Redis caches dashboard summary data, the activity service records task events, PostgreSQL data persists through a Docker volume, and the optional monitoring profile collects metrics from both ASP.NET services.

## Architecture

```text
Browser
  |
  v
Nginx reverse proxy :80
  |-- /             -> React static frontend
  |-- /health       -> ASP.NET Core backend
  |-- /api/tasks    -> ASP.NET Core backend
  `-- /api/activity -> Activity service
                         |
                         v
                    PostgreSQL database
                    Redis cache

Prometheus :9090 -> backend:8080/metrics, activity-service:8081/metrics
Grafana :3000    -> Prometheus datasource and LocalDeploy dashboard
```

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | React + Vite |
| Backend | ASP.NET Core task API |
| Activity | ASP.NET Core activity service |
| Database | PostgreSQL |
| Cache | Redis |
| Reverse proxy | Nginx |
| Monitoring | Prometheus + Grafana |
| Containers | Docker + Docker Compose |
| CI | GitHub Actions |

## Features

- Task dashboard with create, read, update status, and delete actions
- Recent activity feed for task-created, task-updated, and task-deleted events
- Backend health endpoint with database and Redis connectivity status
- Redis-backed task summary cache with PostgreSQL fallback
- Separate activity service with best-effort event delivery
- Optional Prometheus and Grafana monitoring profile
- Pre-provisioned Grafana dashboard for service health, HTTP metrics, cache events, and activity events
- Repeatable PostgreSQL backup, restore verification, and backup cleanup scripts
- Laptop-safe Docker-based k6 load test for read-heavy API traffic
- PostgreSQL schema initialization script
- Persistent database volume
- Nginx single entry point at `http://localhost`
- Docker health checks for database, backend, and Nginx
- Production-style Compose override with security headers and API rate limiting
- GitHub Actions Docker build validation

## Project Structure

```text
.
├── backend/                 # ASP.NET Core API
├── database/init.sql         # PostgreSQL schema and seed data
├── frontend/                 # React + Vite dashboard
├── monitoring/               # Prometheus config and Grafana provisioning
├── services/activity-service/ # ASP.NET Core activity API
├── scripts/                  # Backup and restore scripts
├── tests/load/               # k6 load tests
├── nginx/nginx.conf          # Reverse proxy configuration
├── nginx/nginx.prod.conf     # Production-style reverse proxy configuration
├── docs/screenshots/         # Portfolio screenshots
├── .github/workflows/        # CI workflow
├── docker-compose.yml
├── docker-compose.prod.yml
├── .env.example
└── README.md
```

## Documentation

| Document | Description |
| --- | --- |
| [Architecture](docs/architecture.md) | Service layout, request flow, networking, and persistence |
| [Setup Guide](docs/setup-guide.md) | Prerequisites, environment setup, run/reset commands, and backups |
| [API Reference](docs/api-reference.md) | Endpoint table, curl examples, validation rules, and Swagger |
| [Security Notes](docs/security-notes.md) | Production-style Compose, headers, rate limiting, and secret handling |
| [Monitoring Guide](docs/monitoring-guide.md) | Prometheus/Grafana profile, dashboard, screenshots, and cleanup |
| [Backup And Recovery](docs/backup-and-recovery.md) | Backup, restore, verification, and retention commands |
| [Load Testing](docs/load-testing.md) | Docker-based k6 usage and laptop-safe defaults |
| [Performance Results](docs/performance-results.md) | Fill-in record for local load-test evidence |
| [Azure Deployment Guide](docs/deployment-guide.md) | Azure Student VM deployment with Docker Compose |
| [Cloud Cost Notes](docs/cloud-cost-notes.md) | Student-credit cost controls and cleanup checklist |
| [Troubleshooting](docs/troubleshooting.md) | Common Docker, database, backup, frontend, and CI issues |

## Run Locally

Prerequisites:

- Docker Engine
- Docker Compose plugin

Start the full stack:

```bash
docker compose up --build
```

Open:

```text
http://localhost
```

Stop the stack:

```bash
docker compose down
```

Stop the stack and delete database data:

```bash
docker compose down -v
```

Use `down -v` carefully because it removes the PostgreSQL volume.

## Production-Style Run

Stage V1 adds a stricter local runtime with `docker-compose.prod.yml`. It keeps only Nginx public, requires environment values, uses restart policies, mounts a production Nginx config, disables Swagger by default, and stores data in a separate `postgres_prod_data` volume.

Create a `.env` file first:

```bash
cp .env.example .env
```

Change the password values in `.env`, then start the production-style stack:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Stop it with the same Compose files:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml down
```

Swagger is available in the development stack. It is disabled by default with the production override unless `ENABLE_SWAGGER=true` is set.

## Azure Student Deployment

Stage V6 deploys the same production-style Compose stack to Azure using one Ubuntu VM in an Azure for Students subscription.

Recommended cloud shape:

```text
Azure Portal
Azure for Students
Ubuntu VM
Docker Compose
Nginx public on port 80
Backend, activity-service, Redis, and PostgreSQL internal to Docker
```

Start with [Azure Deployment Guide](docs/deployment-guide.md), then use [Cloud Cost Notes](docs/cloud-cost-notes.md) to keep the deployment student-credit friendly.

## Optional Monitoring

Stage V4 adds Prometheus and Grafana behind the `monitoring` Compose profile. The app still starts normally without monitoring.

Start the stack with monitoring:

```bash
docker compose --profile monitoring up -d --build
```

Open:

```text
http://localhost:9090
http://localhost:3000
```

Grafana uses `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD` from `.env.example` by default. The provisioned dashboard is named `LocalDeploy Platform`.

Stop the stack when you are done testing or taking screenshots:

```bash
docker compose --profile monitoring down
```

## Environment Variables

Copy `.env.example` to `.env` if you want to customize local values:

```bash
cp .env.example .env
```

| Variable | Default | Purpose |
| --- | --- | --- |
| `POSTGRES_DB` | `localdeploydb` | PostgreSQL database name |
| `POSTGRES_USER` | `localdeploy_user` | PostgreSQL user |
| `POSTGRES_PASSWORD` | `localdeploy_password` | PostgreSQL password |
| `DB_HOST` | `database` | Backend database host inside Docker network |
| `DB_PORT` | `5432` | Backend database port |
| `DB_NAME` | `localdeploydb` | Backend database name |
| `DB_USER` | `localdeploy_user` | Backend database user |
| `DB_PASSWORD` | `localdeploy_password` | Backend database password |
| `REDIS_HOST` | `redis` | Backend Redis host inside Docker network |
| `REDIS_PORT` | `6379` | Backend Redis port |
| `REDIS_CACHE_SECONDS` | `60` | Task summary cache TTL |
| `ACTIVITY_SERVICE_URL` | `http://activity-service:8081` | Internal URL used by the task API to record activity |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Backend runtime environment |
| `ENABLE_SWAGGER` | `true` in dev, `false` in production-style mode | Enables Swagger when explicitly set |
| `NGINX_HTTP_PORT` | `80` | Host port used by the production Compose override |
| `PROMETHEUS_PORT` | `9090` | Host port for Prometheus when the monitoring profile is enabled |
| `GRAFANA_PORT` | `3000` | Host port for Grafana when the monitoring profile is enabled |
| `GRAFANA_ADMIN_USER` | `admin` | Local Grafana administrator username |
| `GRAFANA_ADMIN_PASSWORD` | `localdeploy_admin` | Local Grafana administrator password |

## URLs

| URL | Description |
| --- | --- |
| `http://localhost` | React dashboard through Nginx |
| `http://localhost/health` | Backend health check through Nginx |
| `http://localhost/api/tasks` | Task API through Nginx |
| `http://localhost/api/tasks/summary` | Redis-cached task summary counts |
| `http://localhost/api/activity` | Recent activity events through Nginx |
| `http://localhost/swagger` | Swagger/OpenAPI documentation |
| `http://localhost:9090` | Prometheus UI when the monitoring profile is enabled |
| `http://localhost:3000` | Grafana UI when the monitoring profile is enabled |

The backend container listens on port `8080` internally, but it is not exposed directly to the host. Nginx is the public entry point.
The backend and activity service expose `/metrics` only inside the Docker network for Prometheus scraping.

## API Endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/health` | API and database health |
| `GET` | `/api/tasks` | List tasks |
| `GET` | `/api/tasks/summary` | Task summary counts cached in Redis |
| `GET` | `/api/activity` | Latest task activity events |
| `GET` | `/api/tasks/{id}` | Get one task |
| `POST` | `/api/tasks` | Create task |
| `PUT` | `/api/tasks/{id}` | Update task fields |
| `DELETE` | `/api/tasks/{id}` | Delete task |

Example create request:

```bash
curl -X POST http://localhost/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Write README","description":"Document the local lab","priority":"High"}'
```

Validation rules:

- `title` is required when creating a task.
- `title` must be 150 characters or fewer.
- `status` must be one of `Pending`, `In Progress`, `Completed`, or `Blocked`.
- `priority` must be one of `Low`, `Medium`, `High`, or `Critical`.
- Invalid requests return `400 Bad Request` with an `error` and `details` array.

Example invalid request:

```bash
curl -i -X POST http://localhost/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"","priority":"Urgent"}'
```

Example validation response:

```json
{
  "error": "Validation failed",
  "details": [
    "Title is required.",
    "Priority must be one of: Low, Medium, High, Critical."
  ]
}
```

## Docker Services

| Service | Purpose | Public port |
| --- | --- | --- |
| `nginx` | Serves frontend and proxies API requests | `80` |
| `backend` | ASP.NET Core API | Internal only |
| `activity-service` | Records and lists task activity events | Internal only |
| `redis` | Cache for task summary counts | Internal only |
| `database` | PostgreSQL database | Internal only |
| `prometheus` | Metrics collection for the monitoring profile | `9090` when enabled |
| `grafana` | Metrics dashboard for the monitoring profile | `3000` when enabled |

The production Compose override adds `restart: unless-stopped` to all services and keeps backend, activity-service, Redis, and database internal. See [Security Notes](docs/security-notes.md) for details.

Check service status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs
docker compose logs backend
docker compose logs activity-service
docker compose logs redis
docker compose logs database
docker compose logs nginx
docker compose logs prometheus
docker compose logs grafana
```

Follow backend logs while testing API requests:

```bash
docker compose logs -f backend
```

The backend writes readable application logs for health checks, task list/detail requests, task summary cache hits/misses, create/update/delete actions, validation failures, missing task IDs, activity delivery, and database or Redis health failures.

## Database Backup And Restore

Create a timestamped PostgreSQL backup from the running database container:

```bash
./scripts/backup-db.sh
```

Backups are written to `backups/` and are ignored by Git.

Restore a backup into the running database container:

```bash
./scripts/restore-db.sh backups/localdeploydb_YYYYMMDD_HHMMSS.sql
```

Restore resets the current `public` schema before loading the backup. This is destructive for the current local database state, so create a backup first if you need to keep the latest data.

Verify that backup and restore works end to end:

```bash
./scripts/verify-restore.sh
```

Remove old backup files while keeping the newest 7:

```bash
./scripts/cleanup-backups.sh
```

Keep a different number of backups:

```bash
./scripts/cleanup-backups.sh 14
```

## Load Testing

Run the laptop-safe k6 read test through Docker:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway grafana/k6 run - < tests/load/localdeploy-read-load.js
```

If `host.docker.internal` does not resolve on your Linux Docker setup, run with an explicit base URL:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway \
  -e BASE_URL=http://172.17.0.1 \
  grafana/k6 run - < tests/load/localdeploy-read-load.js
```

Record the local result in [Performance Results](docs/performance-results.md).

## Screenshots

Add portfolio screenshots to `docs/screenshots/`:

| Screenshot | Path |
| --- | --- |
| App dashboard | `docs/screenshots/app-dashboard.png` |
| Docker containers | `docs/screenshots/docker-containers.png` |
| Health endpoint | `docs/screenshots/health-endpoint.png` |
| Swagger API docs | `docs/screenshots/swagger-api.png` |
| GitHub Actions passing | `docs/screenshots/github-actions.png` |
| Restore verification | `docs/screenshots/restore-verification.png` |
| k6 load test | `docs/screenshots/k6-load-test.png` |
| Azure VM overview | `docs/screenshots/azure-vm-overview.png` |
| Azure public app | `docs/screenshots/azure-public-app.png` |
| Azure health endpoint | `docs/screenshots/azure-health-endpoint.png` |

## CI

GitHub Actions validates the Docker setup on push and pull request:

- backend validation tests with `dotnet test`
- shell syntax validation for operational scripts
- `docker compose config`
- production Compose validation with `docker compose --env-file .env.example -f docker-compose.yml -f docker-compose.prod.yml config`
- Docker Compose service validation includes Redis and activity-service
- backend Docker image build
- activity-service Docker image build
- frontend/Nginx Docker image build

Workflow file:

```text
.github/workflows/docker-build.yml
```

## Troubleshooting

Port 80 is already in use:

```bash
sudo lsof -i :80
```

Stop the service using port `80`, or temporarily change the Nginx host port in `docker-compose.yml`.

Docker permission denied:

```bash
sudo usermod -aG docker $USER
```

Then log out and log back in.

Database data did not reset:

```bash
docker compose down -v
docker compose up --build
```

Backend cannot connect to database:

- Confirm `DB_HOST=database`.
- Check `docker compose logs database`.
- Check `docker compose ps` and health status.

Backup or restore script cannot reach database:

- Start the stack with `docker compose up -d --build`.
- Confirm `localdeploy-database` is healthy with `docker compose ps`.
- Check the backup file path when restore says the file is missing.
- Check `docker compose logs database` if restore fails while loading SQL.

Frontend cannot call API:

- Use `http://localhost`, not `http://localhost:5173`, for the Dockerized stack.
- Check `nginx/nginx.conf`.
- Check `docker compose logs nginx backend`.

## What This Project Demonstrates

- Multi-container application design
- Docker Compose networking
- Persistent PostgreSQL storage
- Redis caching with graceful fallback
- Best-effort activity event recording with a separate service
- Reverse proxy routing with Nginx
- Production-style Compose hardening
- Nginx security headers and API rate limiting
- Environment-based configuration
- Basic CI validation with GitHub Actions
- Full-stack deployment thinking on a local Linux machine
