# Monitoring Guide

Stage V4 adds optional Prometheus and Grafana monitoring for LocalDeploy Lab.

Monitoring is intentionally profile-based. The normal development stack stays small, and Prometheus/Grafana run only when requested.

## Start Monitoring

```bash
docker compose --profile monitoring up -d --build
```

Open:

```text
http://localhost:9090
http://localhost:3000
```

Grafana uses `GRAFANA_ADMIN_USER` and `GRAFANA_ADMIN_PASSWORD` from `.env` or `.env.example`. The default local values are:

```text
admin / localdeploy_admin
```

## What Gets Scraped

Prometheus scrapes:

- `prometheus:9090`
- `backend:8080/metrics`
- `activity-service:8081/metrics`

The app metrics endpoints stay internal to the Docker network. Nginx does not expose `/metrics`.

## Dashboard

Grafana provisions a Prometheus datasource and the `LocalDeploy Platform` dashboard automatically.

Dashboard panels include:

- Prometheus target health by job
- API request rate by service
- API error rate by service
- P95 request duration by service
- PostgreSQL and Redis dependency status
- Task summary cache hit, miss, and fallback events
- Activity delivery success and failure events
- Activity events recorded by event type

Generate traffic before taking screenshots:

```bash
curl http://localhost/api/tasks
curl http://localhost/api/tasks/summary
curl http://localhost/api/activity
```

Suggested screenshot targets:

- `docs/screenshots/grafana-dashboard.png`
- `docs/screenshots/prometheus-targets.png`

## Production-Style Monitoring

Validate the production-style monitoring config:

```bash
docker compose --env-file .env.example --profile monitoring -f docker-compose.yml -f docker-compose.prod.yml config
```

Start it with your `.env` file:

```bash
docker compose --env-file .env --profile monitoring -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Change `GRAFANA_ADMIN_PASSWORD` before using Grafana in any shared environment.

## Stop Monitoring

On an 8GB machine, stop Prometheus and Grafana after screenshots or testing:

```bash
docker compose --profile monitoring down
```

Use the same Compose flags you used to start the stack when stopping a production-style run.

## V4 Non-goals

V4 does not include cAdvisor, node-exporter, postgres-exporter, redis-exporter, Loki, alerts, paging, or container CPU/memory dashboards. Those can be added later after the base Prometheus/Grafana workflow is comfortable.
