# API Reference

The API is available through Nginx at `http://localhost`. Swagger is available at:

```text
http://localhost/swagger
```

## Endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| `GET` | `/health` | API and database health |
| `GET` | `/api/tasks` | List all tasks |
| `GET` | `/api/tasks/summary` | Task summary counts cached in Redis |
| `GET` | `/api/activity` | Latest task activity events |
| `GET` | `/api/tasks/{id}` | Get one task by ID |
| `POST` | `/api/tasks` | Create a task |
| `PUT` | `/api/tasks/{id}` | Update task fields |
| `DELETE` | `/api/tasks/{id}` | Delete a task |

## Health

```bash
curl http://localhost/health
```

Example response:

```json
{
  "status": "running",
  "service": "localdeploy-api",
  "database": "connected",
  "redis": "connected"
}
```

## List Tasks

```bash
curl http://localhost/api/tasks
```

## Task Summary

```bash
curl http://localhost/api/tasks/summary
```

Example response:

```json
{
  "total": 4,
  "pending": 1,
  "inProgress": 1,
  "completed": 1,
  "blocked": 1
}
```

The backend caches this response in Redis for `60` seconds by default. Successful task create, update, and delete operations clear the cache so the next summary request refreshes from PostgreSQL. If Redis is unavailable, the endpoint still returns counts from PostgreSQL.

## Activity

```bash
curl http://localhost/api/activity
```

Example response:

```json
[
  {
    "id": 1,
    "eventType": "task-created",
    "taskId": 12,
    "taskTitle": "Write docs",
    "message": "Task created: Write docs",
    "createdAt": "2026-04-30T16:30:00"
  }
]
```

The activity service stores task-created, task-updated, and task-deleted events in PostgreSQL. Task mutations use best-effort delivery, so task operations still succeed if the activity service is temporarily unavailable.

## Create Task

```bash
curl -X POST http://localhost/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Write docs","description":"Add API reference","priority":"High","status":"Pending"}'
```

Required and optional fields:

| Field | Required | Notes |
| --- | --- | --- |
| `title` | Yes | Maximum 150 characters |
| `description` | No | Can be empty |
| `status` | No | Defaults to `Pending` |
| `priority` | No | Defaults to `Medium` |

Allowed statuses:

```text
Pending, In Progress, Completed, Blocked
```

Allowed priorities:

```text
Low, Medium, High, Critical
```

## Update Task

```bash
curl -X PUT http://localhost/api/tasks/1 \
  -H "Content-Type: application/json" \
  -d '{"status":"Completed","priority":"Medium"}'
```

Only fields included in the request are updated.

## Delete Task

```bash
curl -X DELETE http://localhost/api/tasks/1
```

Successful delete returns `204 No Content`.

## Validation Errors

Invalid requests return `400 Bad Request`:

```bash
curl -i -X POST http://localhost/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"","priority":"Urgent"}'
```

Example response:

```json
{
  "error": "Validation failed",
  "details": [
    "Title is required.",
    "Priority must be one of: Low, Medium, High, Critical."
  ]
}
```

Missing task IDs return `404 Not Found`.

## Metrics

Both ASP.NET services expose Prometheus metrics internally when running in Docker:

```text
backend:8080/metrics
activity-service:8081/metrics
```

These endpoints are not routed through Nginx. Use the optional monitoring profile to scrape them:

```bash
docker compose --profile monitoring up -d --build
```
