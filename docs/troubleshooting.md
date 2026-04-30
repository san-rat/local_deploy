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

## Frontend Cannot Call API

For the Dockerized stack, use:

```text
http://localhost
```

The frontend should call relative paths like `/api/tasks`. Nginx proxies those requests to the backend.

## GitHub Actions Fails

The workflow runs backend tests, Compose validation, and Docker image builds. Common checks:

- Confirm `dotnet test backend/LocalDeploy.Api.Tests/LocalDeploy.Api.Tests.csproj --configuration Release` passes locally.
- Confirm `docker compose config` passes locally.
- Confirm Dockerfiles build locally with `docker compose build`.
