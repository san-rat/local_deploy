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
  `-- /swagger      -> backend:8080/swagger
                         |
                         v
                    PostgreSQL database
                    postgres_data volume
```

## Services

| Service | Role | Public access |
| --- | --- | --- |
| `nginx` | Serves the React build and proxies API traffic | `http://localhost` |
| `backend` | ASP.NET Core task API | Internal Docker network only |
| `database` | PostgreSQL storage | Internal Docker network only |

## Request Flow

Frontend page loads from `http://localhost`. API calls use relative paths such as `/api/tasks`, so the browser never needs to know the backend container name or port.

Nginx proxies `/api/`, `/health`, and `/swagger/` requests to the backend service over the Compose network. The backend connects to PostgreSQL through the Compose service name `database`.

## Persistence

PostgreSQL data is stored in the named Docker volume `postgres_data`. Running `docker compose down` keeps the data. Running `docker compose down -v` removes it.

The database schema and seed task are created from `database/init.sql` only when PostgreSQL initializes a fresh volume.

## Health Checks

Compose health checks start services in dependency order:

- `database` waits for `pg_isready`.
- `backend` waits for `/health`.
- `nginx` waits for `/health` through the proxy.

This keeps the stack predictable during local startup.
