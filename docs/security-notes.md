# Security Notes

This project is a local DevOps lab, not a complete production security baseline. Stage V1 adds production-style hardening that is useful to understand, test, and explain before a real cloud deployment.

## Production Compose

Use the production override when you want a stricter local runtime:

```bash
docker compose --env-file .env -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

The production override:

- Adds `restart: unless-stopped` for all services.
- Requires database environment variables instead of relying on implicit defaults.
- Keeps backend and database internal to the Compose network.
- Uses a separate `postgres_prod_data` volume from the development `postgres_data` volume.
- Sets the backend environment to `Production`.
- Disables Swagger by default with `ENABLE_SWAGGER=false`.

Do not commit `.env`. Copy `.env.example` to `.env`, then change the password values before using the production-style stack in any shared or deployed environment.

## Public Entry Point

Nginx is the only public service. The backend listens on port `8080` inside Docker only, and PostgreSQL is only reachable through the Compose network.

Public routes are:

```text
/              React frontend
/health        Backend health through Nginx
/api/tasks     Task API through Nginx
/swagger       Swagger only when explicitly enabled
```

## Security Headers

The production Nginx config includes:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy` with a self-only baseline for the React app

These headers reduce common browser-side risks. They are intentionally conservative for the current frontend.

## Rate Limiting

The production Nginx config applies basic rate limiting to `/api/`:

```text
10 requests per second per client IP
20 request burst
429 Too Many Requests for rejected bursts
```

The `/health` endpoint is not rate limited so Docker health checks and simple uptime checks stay reliable.

## Swagger

Swagger remains enabled in the development stack for learning and demos:

```text
http://localhost/swagger
```

The production override disables Swagger by default. To temporarily expose it for a controlled demo, set:

```env
ENABLE_SWAGGER=true
```

Then restart the production-style stack.

## Known Non-goals In Stage V1

Stage V1 does not add:

- HTTPS certificates
- Authentication or authorization
- Secret managers
- Container image vulnerability scanning
- Resource limits tuned from monitoring data
- Redis, Prometheus, Grafana, or cloud deployment

Those belong to later very advanced stages.
