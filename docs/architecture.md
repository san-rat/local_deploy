# Architecture

LocalDeploy Lab is a small production-style local stack. The browser talks to one public entry point, Nginx, and every backend dependency stays inside the Docker Compose network.

```text
Browser
  |
  v
Nginx :80
  |-- /             -> React static files
  |-- /health       -> backend:8080/health
  |-- /api/tasks    -> backend:8080/api/tasks
  |-- /api/activity -> activity-service:8081/api/activity
  `-- /swagger      -> backend:8080/swagger
                         |
                         v
                    PostgreSQL database
                    Redis cache
```

## Services

| Service | Role | Public access |
| --- | --- | --- |
| `nginx` | Serves the React build and proxies API traffic | `http://localhost` |
| `backend` | ASP.NET Core task API | Internal Docker network only |
| `activity-service` | Records and lists task activity events | Internal Docker network only |
| `redis` | Cache for task summary counts | Internal Docker network only |
| `database` | PostgreSQL storage | Internal Docker network only |

## Request Flow

Frontend page loads from `http://localhost`. API calls use relative paths such as `/api/tasks`, so the browser never needs to know the backend container name or port.

Nginx proxies `/api/tasks`, `/health`, and `/swagger/` requests to the backend service over the Compose network. It proxies `/api/activity` to the activity service. The backend connects to PostgreSQL through the Compose service name `database`, Redis through `redis`, and the activity service through `activity-service`.

## Activity Flow

The task API sends best-effort internal events to `POST /internal/activity` after successful task create, update, and delete operations.

The activity service stores events in PostgreSQL table `activity_events`. The dashboard activity feed calls `GET /api/activity`, which Nginx routes to the activity service. If the activity service is unavailable, task mutations still succeed and the task backend logs the delivery failure.

## Redis Cache Flow

The dashboard summary cards call `/api/tasks/summary`.

The backend checks Redis key `tasks:summary` first. On a cache hit, it returns the cached counts. On a miss, it reads counts from PostgreSQL, stores the summary in Redis for `60` seconds, and returns the fresh response.

Successful task create, update, and delete operations delete `tasks:summary` so the next summary request refreshes the cache. If Redis is unavailable, the backend logs a warning and reads the summary directly from PostgreSQL.

## Production-Style Runtime

The default `docker-compose.yml` remains optimized for local development and portfolio demos. Stage V1 adds `docker-compose.prod.yml` as a production-style override:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

The production override keeps only Nginx public, adds restart policies, requires database, Redis, and activity-service environment variables, sets services to `Production`, disables Swagger by default, and mounts a hardened Nginx config with security headers and API rate limiting.

## Persistence

PostgreSQL data is stored in the named Docker volume `postgres_data`. Running `docker compose down` keeps the data. Running `docker compose down -v` removes it.

The database schema and seed task are created from `database/init.sql` only when PostgreSQL initializes a fresh volume.

The production-style stack uses a separate `postgres_prod_data` volume so production-style testing does not accidentally reuse the development database volume.

Redis has no persistent volume in this project because it stores cache data only.

The activity service creates `activity_events` with `CREATE TABLE IF NOT EXISTS` during startup, so existing local PostgreSQL volumes do not need to be reset.

## Health Checks

Compose health checks start services in dependency order:

- `database` waits for `pg_isready`.
- `redis` waits for `redis-cli ping`.
- `backend` waits for `/health`.
- `activity-service` waits for `/health`.
- `nginx` waits for `/health` through the proxy.

This keeps the stack predictable during local startup.
