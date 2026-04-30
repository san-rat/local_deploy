# LocalDeploy Lab — Advanced Version Guide

## 1. Purpose

The minimum LocalDeploy Lab proves that a full-stack system can run locally with Docker Compose:

```text
React frontend
ASP.NET Core API
PostgreSQL database
Nginx reverse proxy
Docker Compose
GitHub Actions build validation
```

The advanced version should make the same system more professional, maintainable, testable, and easier to explain in interviews.

This is not the very advanced version. Avoid adding heavy infrastructure such as Kubernetes, Prometheus, Grafana, Redis, cloud deployment, HTTPS domains, or multiple backend services for now.

---

## 2. Current Minimum Version Status

Already completed:

```text
[x] React dashboard
[x] ASP.NET Core task API
[x] PostgreSQL persistence
[x] Nginx single entry point at http://localhost
[x] Docker Compose stack
[x] Persistent PostgreSQL volume
[x] Docker health checks
[x] GitHub Actions Docker build workflow
[x] README with run instructions and architecture
[x] docs/screenshots folder
```

The advanced version should build on top of this, not rewrite it.

---

## 3. Advanced Version Goal

Goal:

```text
Turn the working local lab into a cleaner professional project with API documentation, stronger backend behavior, better logs, test coverage, operational scripts, and more complete documentation.
```

Advanced version success means:

```text
The project is easy to run, easy to inspect, easy to debug, and easy to explain.
```

---

## 4. What The Advanced Version Should Add

## 4.1 Swagger / OpenAPI Through Nginx

Current backend already includes Swagger packages, but Swagger is only enabled in development mode and is not yet exposed cleanly through Nginx.

Advanced target:

```text
http://localhost/swagger
```

Expected result:

```text
Swagger UI displays all task API endpoints.
Requests can be tested from the browser.
The README links to the Swagger page.
```

Recommended work:

```text
[ ] Configure Swagger metadata: title, version, description
[ ] Make Swagger available in the Dockerized local lab
[ ] Add Nginx route for /swagger and /swagger/v1/swagger.json if needed
[ ] Add screenshot placeholder: docs/screenshots/swagger-api.png
```

---

## 4.2 Backend Validation

The backend should reject invalid task data before writing to PostgreSQL.

Rules:

```text
Title is required
Title max length is 150 characters
Status must be one of: Pending, In Progress, Completed, Blocked
Priority must be one of: Low, Medium, High, Critical
Description can be empty
```

Expected invalid response shape:

```json
{
  "error": "Validation failed",
  "details": [
    "Title is required"
  ]
}
```

Recommended work:

```text
[ ] Validate POST /api/tasks
[ ] Validate PUT /api/tasks/{id}
[ ] Return 400 Bad Request for invalid input
[ ] Keep 404 Not Found for missing task IDs
[ ] Add curl examples to docs/api-reference.md
```

---

## 4.3 Error Handling

Advanced target:

```text
API errors should be predictable and readable.
```

Recommended response patterns:

```text
400 Bad Request       validation errors
404 Not Found         task does not exist
500 Internal Error    unexpected server/database failure
```

Recommended work:

```text
[ ] Add a small helper for validation error responses
[ ] Avoid exposing raw exception details to clients
[ ] Log unexpected exceptions on the backend
[ ] Keep response JSON consistent
```

---

## 4.4 Structured Logging

Current Docker logs show service startup and framework logs. The advanced version should add useful application-level logs.

Useful backend logs:

```text
Health check requested
Task list requested
Task created
Task updated
Task deleted
Validation failed
Database connection failed
Unexpected error occurred
```

Recommended work:

```text
[ ] Use ASP.NET Core ILogger
[ ] Log key task operations with task ID where relevant
[ ] Log validation failures at warning level
[ ] Log unexpected errors at error level
[ ] Add README examples for docker compose logs backend
```

---

## 4.5 Backend Tests

The current CI checks Docker builds. The advanced version should add at least basic backend tests.

Recommended test project:

```text
backend/LocalDeploy.Api.Tests/
```

Recommended test coverage:

```text
[ ] Validation accepts valid task input
[ ] Validation rejects empty title
[ ] Validation rejects invalid status
[ ] Validation rejects invalid priority
[ ] Status transition helper works if one is introduced
```

CI improvement:

```text
[ ] Run dotnet test in GitHub Actions
```

Keep tests focused. Do not build a large test suite yet.

---

## 4.6 Frontend Polish

The dashboard already works. The advanced version should make it feel more complete and screenshot-ready.

Recommended improvements:

```text
[ ] Add status filter controls
[ ] Add priority filter controls
[ ] Add clearer empty state
[ ] Add per-action loading state for update/delete
[ ] Improve form validation before submit
[ ] Show last updated time
```

Avoid adding a large UI framework unless there is a clear need.

---

## 4.7 Database Backup And Restore Scripts

Add basic operational scripts to show database maintenance awareness.

Recommended files:

```text
scripts/backup-db.sh
scripts/restore-db.sh
backups/
```

Backup behavior:

```text
Create a timestamped .sql backup from the running PostgreSQL container.
Store it in backups/.
```

Restore behavior:

```text
Restore a chosen .sql file into the running PostgreSQL container.
Require a filename argument.
```

Git ignore:

```gitignore
backups/
```

Recommended work:

```text
[ ] Add backup script
[ ] Add restore script
[ ] Document usage in README or docs/setup-guide.md
[ ] Test backup and restore locally
```

---

## 4.8 Split Documentation Into Focused Files

The README is good for the project front page. Advanced documentation should add deeper docs without making the README too long.

Recommended files:

```text
docs/architecture.md
docs/setup-guide.md
docs/api-reference.md
docs/troubleshooting.md
```

Recommended contents:

```text
architecture.md      service diagram, request flow, networking notes
setup-guide.md       install prerequisites, run commands, reset commands
api-reference.md     endpoint table, request examples, response examples
troubleshooting.md   common Docker, Nginx, DB, and CI issues
```

---

## 4.9 Screenshot Evidence

Screenshots can be added after the advanced work is implemented.

Recommended screenshots:

```text
docs/screenshots/app-dashboard.png
docs/screenshots/docker-containers.png
docs/screenshots/health-endpoint.png
docs/screenshots/swagger-api.png
docs/screenshots/github-actions.png
docs/screenshots/backup-script.png
```

README should show or link these screenshots.

---

## 5. Recommended Advanced Version Stages

## Stage A1 — Swagger Through Nginx

Deliverable:

```text
Swagger UI is available at http://localhost/swagger.
```

Checklist:

```text
[ ] Configure Swagger metadata
[ ] Enable Swagger for local Docker lab
[ ] Add Nginx routing if required
[ ] Test Swagger in browser
[ ] Update README with Swagger URL
```

---

## Stage A2 — Validation And Error Handling

Deliverable:

```text
Invalid API requests return clear 400 responses.
```

Checklist:

```text
[ ] Add validation rules
[ ] Add consistent error response helper
[ ] Test invalid POST requests
[ ] Test invalid PUT requests
[ ] Document error responses
```

---

## Stage A3 — Structured Backend Logging

Deliverable:

```text
Backend logs clearly show important API activity.
```

Checklist:

```text
[ ] Add ILogger usage
[ ] Log create/update/delete operations
[ ] Log validation failures
[ ] Log database connection failures
[ ] Verify logs with docker compose logs backend
```

---

## Stage A4 — Backend Test Project And CI Test Step

Deliverable:

```text
GitHub Actions runs backend tests in addition to Docker builds.
```

Checklist:

```text
[ ] Add backend test project
[ ] Add validation-focused tests
[ ] Run dotnet test locally
[ ] Add dotnet test to GitHub Actions
[ ] Confirm CI passes
```

---

## Stage A5 — Backup And Restore Scripts

Deliverable:

```text
Database can be backed up and restored using scripts.
```

Checklist:

```text
[ ] Add scripts/backup-db.sh
[ ] Add scripts/restore-db.sh
[ ] Add backups/ to .gitignore
[ ] Test backup script
[ ] Test restore script
[ ] Document commands
```

---

## Stage A6 — Documentation Expansion

Deliverable:

```text
Project has dedicated architecture, setup, API, and troubleshooting docs.
```

Checklist:

```text
[ ] Add docs/architecture.md
[ ] Add docs/setup-guide.md
[ ] Add docs/api-reference.md
[ ] Add docs/troubleshooting.md
[ ] Link docs from README
[ ] Add screenshots when available
```

---

## 6. Advanced Version Completion Checklist

```text
[ ] Minimum Docker stack still works with docker compose up --build
[ ] Swagger works through Nginx
[ ] Backend validation rejects invalid data
[ ] Error responses are consistent
[ ] Backend logs useful application events
[ ] Backend tests exist and pass
[ ] GitHub Actions runs Docker builds and tests
[ ] Backup and restore scripts work
[ ] README links to deeper docs
[ ] Troubleshooting guide exists
[ ] Screenshots are added
```

---

## 7. What To Avoid In This Advanced Version

Avoid these for now:

```text
Kubernetes
Prometheus and Grafana
Redis
Cloud deployment
HTTPS and domain name
Multiple backend services
Centralized logging stacks
Blue-green deployment
```

Those belong to the very advanced version.

---

## 8. Suggested CV Description After Advanced Version

Long version:

```text
Enhanced a Dockerized full-stack DevOps lab with Swagger API documentation, backend validation, structured logging, Docker health checks, GitHub Actions CI, PostgreSQL backup and restore scripts, and detailed architecture/setup/troubleshooting documentation.
```

Short version:

```text
Extended a Dockerized React, ASP.NET Core, PostgreSQL, and Nginx lab with API docs, validation, logging, CI checks, and database backup scripts.
```
