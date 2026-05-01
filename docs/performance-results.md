# Performance Results

Use this page to record local k6 results for portfolio evidence.

## Latest Local Run

| Field | Value |
| --- | --- |
| Date | 2026-05-01 |
| Machine | Ubuntu 24.04.4 LTS, Linux 6.17.0-22-generic, x86_64, 7.6Gi RAM |
| Stack mode | Development Compose |
| Command | `docker run --rm -i --add-host=host.docker.internal:host-gateway grafana/k6 run - < tests/load/localdeploy-read-load.js` |
| k6 script | `tests/load/localdeploy-read-load.js` |
| Result | Pass |
| HTTP failure rate | 0.00%, 0 out of 600 |
| P95 request duration | 3.87 ms |
| Checks passed | 100.00%, 600 out of 600 |
| Notes | 5 max VUs, 150 complete iterations, read-only traffic through Nginx |

## Screenshot Targets

```text
docs/screenshots/k6-load-test.png
docs/screenshots/restore-verification.png
```

## Interpretation

The V5 load test is intentionally laptop-safe and read-heavy. Treat results as local confidence evidence, not production capacity numbers.
