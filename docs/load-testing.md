# Load Testing

Stage V5 adds a laptop-safe k6 read test for LocalDeploy Lab.

The test is intentionally read-heavy so it exercises the stack without creating or deleting demo data.

## Start The Stack

```bash
docker compose up -d --build
curl http://localhost/health
```

## Run k6 With Docker

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway grafana/k6 run - < tests/load/localdeploy-read-load.js
```

The script uses this default base URL:

```text
http://host.docker.internal
```

If that host name does not work on your Linux Docker setup, use the Docker bridge gateway:

```bash
docker run --rm -i --add-host=host.docker.internal:host-gateway \
  -e BASE_URL=http://172.17.0.1 \
  grafana/k6 run - < tests/load/localdeploy-read-load.js
```

## Test Shape

The V5 test calls:

- `/health`
- `/api/tasks`
- `/api/tasks/summary`
- `/api/activity`

Default load profile:

- 10 second ramp-up to 3 virtual users
- 30 seconds at 5 virtual users
- 10 second ramp-down

Thresholds:

- HTTP failure rate below 1%
- P95 request duration below 500 ms

## Record Results

Copy the important k6 summary values into [Performance Results](performance-results.md), including date, machine notes, stack mode, request duration, failure rate, and checks.

Suggested screenshot target:

```text
docs/screenshots/k6-load-test.png
```

## Limits

This is not a stress test. It is small, repeatable evidence that the local stack can handle light read traffic on an 8GB machine. Later stages can add heavier profiles, write-path tests, distributed execution, and Prometheus-backed load dashboards.
