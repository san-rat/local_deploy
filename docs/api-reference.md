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
  "database": "connected"
}
```

## List Tasks

```bash
curl http://localhost/api/tasks
```

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
