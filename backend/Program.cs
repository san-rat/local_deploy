using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LocalDeploy Lab API",
        Version = "v1",
        Description = "Task management API for the Dockerized LocalDeploy Lab home lab."
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var swaggerEnabled = app.Environment.IsDevelopment()
    || string.Equals(
        Environment.GetEnvironmentVariable("ENABLE_SWAGGER"),
        "true",
        StringComparison.OrdinalIgnoreCase
    );

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalDeploy Lab API v1");
    });
}

app.UseCors("LocalFrontend");

var logger = app.Logger;

string GetConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "localdeploydb";
    var username = Environment.GetEnvironmentVariable("DB_USER") ?? "localdeploy_user";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "localdeploy_password";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};GSS Encryption Mode=Disable";
}

TaskItem ReadTask(NpgsqlDataReader reader)
{
    return new TaskItem(
        reader.GetInt32(reader.GetOrdinal("id")),
        reader.GetString(reader.GetOrdinal("title")),
        reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
        reader.GetString(reader.GetOrdinal("status")),
        reader.GetString(reader.GetOrdinal("priority")),
        reader.GetDateTime(reader.GetOrdinal("created_at")),
        reader.GetDateTime(reader.GetOrdinal("updated_at"))
    );
}

IResult ValidationError(string endpointName, IReadOnlyList<string> details)
{
    logger.LogWarning(
        "Validation failed for {EndpointName}: {ValidationDetails}",
        endpointName,
        string.Join("; ", details)
    );

    return Results.BadRequest(new ValidationErrorResponse("Validation failed", details));
}

app.MapGet("/health", async () =>
{
    try
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();

        logger.LogInformation("Health check requested. Database status: {DatabaseStatus}", "connected");

        return Results.Ok(new
        {
            status = "running",
            service = "localdeploy-api",
            database = "connected"
        });
    }
    catch (Exception exception)
    {
        logger.LogWarning(
            exception,
            "Health check requested. Database status: {DatabaseStatus}",
            "disconnected"
        );

        return Results.Ok(new
        {
            status = "running",
            service = "localdeploy-api",
            database = "disconnected"
        });
    }
})
.WithName("GetHealth")
.WithTags("Health")
.WithOpenApi(operation =>
{
    operation.Summary = "Check API and database health";
    operation.Description = "Returns API status and whether the backend can connect to PostgreSQL.";
    return operation;
});

app.MapGet("/api/tasks", async () =>
{
    var tasks = new List<TaskItem>();

    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        """
        SELECT id, title, description, status, priority, created_at, updated_at
        FROM tasks
        ORDER BY id;
        """,
        connection
    );

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        tasks.Add(ReadTask(reader));
    }

    logger.LogInformation("Task list requested. Returned {TaskCount} tasks", tasks.Count);

    return Results.Ok(tasks);
})
.WithName("GetTasks")
.WithTags("Tasks")
.WithOpenApi(operation =>
{
    operation.Summary = "List tasks";
    operation.Description = "Returns all task records ordered by ID.";
    return operation;
});

app.MapGet("/api/tasks/{id:int}", async (int id) =>
{
    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        """
        SELECT id, title, description, status, priority, created_at, updated_at
        FROM tasks
        WHERE id = @id;
        """,
        connection
    );

    command.Parameters.AddWithValue("id", id);

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        logger.LogWarning("Task requested. Task ID {TaskId} was not found", id);

        return Results.NotFound();
    }

    var task = ReadTask(reader);

    logger.LogInformation("Task requested. Task ID {TaskId} was found", id);

    return Results.Ok(task);
})
.WithName("GetTaskById")
.WithTags("Tasks")
.WithOpenApi(operation =>
{
    operation.Summary = "Get one task";
    operation.Description = "Returns a single task by ID, or 404 if it does not exist.";
    return operation;
});

app.MapPost("/api/tasks", async (CreateTaskRequest request) =>
{
    var validationErrors = TaskValidation.ValidateCreate(request.Title, request.Status, request.Priority);

    if (validationErrors.Count > 0)
    {
        return ValidationError("CreateTask", validationErrors);
    }

    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        """
        INSERT INTO tasks (title, description, status, priority)
        VALUES (@title, @description, @status, @priority)
        RETURNING id, title, description, status, priority, created_at, updated_at;
        """,
        connection
    );

    command.Parameters.AddWithValue("title", request.Title.Trim());
    command.Parameters.AddWithValue("description", request.Description?.Trim() ?? "");
    command.Parameters.AddWithValue("status", request.Status?.Trim() ?? "Pending");
    command.Parameters.AddWithValue("priority", request.Priority?.Trim() ?? "Medium");

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    var task = ReadTask(reader);

    logger.LogInformation(
        "Task created. Task ID {TaskId}, Status {TaskStatus}, Priority {TaskPriority}",
        task.Id,
        task.Status,
        task.Priority
    );

    return Results.Created($"/api/tasks/{task.Id}", task);
})
.WithName("CreateTask")
.WithTags("Tasks")
.WithOpenApi(operation =>
{
    operation.Summary = "Create task";
    operation.Description = "Creates a task with a required title, optional description, and valid status/priority values.";
    return operation;
});

app.MapPut("/api/tasks/{id:int}", async (int id, UpdateTaskRequest request) =>
{
    var validationErrors = TaskValidation.ValidateUpdate(request.Title, request.Status, request.Priority);

    if (validationErrors.Count > 0)
    {
        return ValidationError("UpdateTask", validationErrors);
    }

    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        """
        UPDATE tasks
        SET
            title = COALESCE(@title, title),
            description = COALESCE(@description, description),
            status = COALESCE(@status, status),
            priority = COALESCE(@priority, priority),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = @id
        RETURNING id, title, description, status, priority, created_at, updated_at;
        """,
        connection
    );

    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue("title", request.Title is null ? DBNull.Value : request.Title.Trim());
    command.Parameters.AddWithValue("description", request.Description is null ? DBNull.Value : request.Description.Trim());
    command.Parameters.AddWithValue("status", request.Status is null ? DBNull.Value : request.Status.Trim());
    command.Parameters.AddWithValue("priority", request.Priority is null ? DBNull.Value : request.Priority.Trim());

    await using var reader = await command.ExecuteReaderAsync();

    if (!await reader.ReadAsync())
    {
        logger.LogWarning("Task update requested. Task ID {TaskId} was not found", id);

        return Results.NotFound();
    }

    var task = ReadTask(reader);

    logger.LogInformation(
        "Task updated. Task ID {TaskId}, Status {TaskStatus}, Priority {TaskPriority}",
        task.Id,
        task.Status,
        task.Priority
    );

    return Results.Ok(task);
})
.WithName("UpdateTask")
.WithTags("Tasks")
.WithOpenApi(operation =>
{
    operation.Summary = "Update task";
    operation.Description = "Updates task fields by ID after validating provided title, status, and priority values.";
    return operation;
});

app.MapDelete("/api/tasks/{id:int}", async (int id) =>
{
    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        """
        DELETE FROM tasks
        WHERE id = @id;
        """,
        connection
    );

    command.Parameters.AddWithValue("id", id);

    var deletedRows = await command.ExecuteNonQueryAsync();

    if (deletedRows == 0)
    {
        logger.LogWarning("Task delete requested. Task ID {TaskId} was not found", id);

        return Results.NotFound();
    }

    logger.LogInformation("Task deleted. Task ID {TaskId}", id);

    return Results.NoContent();
})
.WithName("DeleteTask")
.WithTags("Tasks")
.WithOpenApi(operation =>
{
    operation.Summary = "Delete task";
    operation.Description = "Deletes a task by ID, or returns 404 if the task does not exist.";
    return operation;
});

app.Run();

record TaskItem(
    int Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

record CreateTaskRequest(
    string Title,
    string? Description,
    string? Status,
    string? Priority
);

record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    string? Priority
);

record ValidationErrorResponse(
    string Error,
    IReadOnlyList<string> Details
);
