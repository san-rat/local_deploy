# LocalDeploy Lab

A lightweight DevOps home lab that runs a React frontend, ASP.NET Core API, PostgreSQL database, and Nginx reverse proxy with Docker Compose on Ubuntu Linux.

The goal is to show a small production-style full-stack system, not only an application. The browser uses one entry point, Nginx routes API traffic internally, and PostgreSQL data persists through a Docker volume.

## Architecture

```text
Browser
  |
  v
Nginx reverse proxy :80
  |-- /          -> React static frontend
  |-- /health    -> ASP.NET Core backend
  `-- /api/tasks -> ASP.NET Core backend
                       |
                       v
                  PostgreSQL database
                  postgres_data volume
```

## Tech Stack

| Layer | Technology |
| --- | --- |
| Frontend | React + Vite |
| Backend | ASP.NET Core Web API |
| Database | PostgreSQL |
| Reverse proxy | Nginx |
| Containers | Docker + Docker Compose |
| CI | GitHub Actions |

## Features

- Task dashboard with create, read, update status, and delete actions
- Backend health endpoint with database connectivity status
- PostgreSQL schema initialization script
- Persistent database volume
- Nginx single entry point at `http://localhost`
- Docker health checks for database, backend, and Nginx
- GitHub Actions Docker build validation

## Project Structure

```text
.
├── backend/                 # ASP.NET Core API
├── database/init.sql         # PostgreSQL schema and seed data
├── frontend/                 # React + Vite dashboard
├── nginx/nginx.conf          # Reverse proxy configuration
├── docs/screenshots/         # Portfolio screenshots
├── .github/workflows/        # CI workflow
├── docker-compose.yml
├── .env.example
└── README.md
```

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

## URLs

| URL | Description |
| --- | --- |
| `http://localhost` | React dashboard through Nginx |
| `http://localhost/health` | Backend health check through Nginx |
| `http://localhost/api/tasks` | Task API through Nginx |
| `http://localhost/swagger` | Swagger/OpenAPI documentation |

The backend container listens on port `8080` internally, but it is not exposed directly to the host. Nginx is the public entry point.

## API Endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/health` | API and database health |
| `GET` | `/api/tasks` | List tasks |
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
| `database` | PostgreSQL database | Internal only |

Check service status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs
docker compose logs backend
docker compose logs database
docker compose logs nginx
```

Follow backend logs while testing API requests:

```bash
docker compose logs -f backend
```

The backend writes readable application logs for health checks, task list/detail requests, create/update/delete actions, validation failures, missing task IDs, and database health failures.

## Screenshots

Add portfolio screenshots to `docs/screenshots/`:

| Screenshot | Path |
| --- | --- |
| App dashboard | `docs/screenshots/app-dashboard.png` |
| Docker containers | `docs/screenshots/docker-containers.png` |
| Health endpoint | `docs/screenshots/health-endpoint.png` |
| Swagger API docs | `docs/screenshots/swagger-api.png` |
| GitHub Actions passing | `docs/screenshots/github-actions.png` |

## CI

GitHub Actions validates the Docker setup on push and pull request:

- backend validation tests with `dotnet test`
- `docker compose config`
- backend Docker image build
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

Frontend cannot call API:

- Use `http://localhost`, not `http://localhost:5173`, for the Dockerized stack.
- Check `nginx/nginx.conf`.
- Check `docker compose logs nginx backend`.

## What This Project Demonstrates

- Multi-container application design
- Docker Compose networking
- Persistent PostgreSQL storage
- Reverse proxy routing with Nginx
- Environment-based configuration
- Basic CI validation with GitHub Actions
- Full-stack deployment thinking on a local Linux machine
