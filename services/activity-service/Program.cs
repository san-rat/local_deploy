using Npgsql;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var logger = app.Logger;
var dependencyUp = Metrics.CreateGauge(
    "localdeploy_dependency_up",
    "Whether a LocalDeploy dependency is reachable from the app service.",
    new GaugeConfiguration
    {
        LabelNames = new[] { "service" }
    }
);
var activityEventsRecorded = Metrics.CreateCounter(
    "localdeploy_activity_events_recorded_total",
    "Activity events recorded by event type.",
    new CounterConfiguration
    {
        LabelNames = new[] { "event_type" }
    }
);

dependencyUp.WithLabels("postgres").Set(0);

app.UseHttpMetrics(options =>
{
    options.ReduceStatusCodeCardinality();
});

string GetConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "localdeploydb";
    var username = Environment.GetEnvironmentVariable("DB_USER") ?? "localdeploy_user";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "localdeploy_password";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};GSS Encryption Mode=Disable";
}

async Task EnsureActivityTableAsync()
{
    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();
    dependencyUp.WithLabels("postgres").Set(1);

    await using var command = new NpgsqlCommand(
        """
        CREATE TABLE IF NOT EXISTS activity_events (
            id SERIAL PRIMARY KEY,
            event_type VARCHAR(50) NOT NULL,
            task_id INTEGER,
            task_title VARCHAR(150) NOT NULL,
            message TEXT NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        """,
        connection
    );

    await command.ExecuteNonQueryAsync();
}

ActivityEvent ReadActivityEvent(NpgsqlDataReader reader)
{
    return new ActivityEvent(
        reader.GetInt32(reader.GetOrdinal("id")),
        reader.GetString(reader.GetOrdinal("event_type")),
        reader.IsDBNull(reader.GetOrdinal("task_id")) ? null : reader.GetInt32(reader.GetOrdinal("task_id")),
        reader.GetString(reader.GetOrdinal("task_title")),
        reader.GetString(reader.GetOrdinal("message")),
        reader.GetDateTime(reader.GetOrdinal("created_at"))
    );
}

List<string> ValidateActivityEvent(CreateActivityEventRequest request)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.EventType))
    {
        errors.Add("Event type is required.");
    }

    if (string.IsNullOrWhiteSpace(request.TaskTitle))
    {
        errors.Add("Task title is required.");
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        errors.Add("Message is required.");
    }

    return errors;
}

await EnsureActivityTableAsync();
logger.LogInformation("Activity events table is ready");

app.MapGet("/health", async () =>
{
    try
    {
        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync();
        dependencyUp.WithLabels("postgres").Set(1);

        logger.LogInformation("Activity service health check requested. Database status: {DatabaseStatus}", "connected");

        return Results.Ok(new
        {
            status = "running",
            service = "localdeploy-activity-service",
            database = "connected"
        });
    }
    catch (Exception exception)
    {
        dependencyUp.WithLabels("postgres").Set(0);
        logger.LogWarning(exception, "Activity service health check failed");

        return Results.Ok(new
        {
            status = "running",
            service = "localdeploy-activity-service",
            database = "disconnected"
        });
    }
});

app.MapMetrics("/metrics");

app.MapGet("/api/activity", async () =>
{
    var events = new List<ActivityEvent>();

    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();
    dependencyUp.WithLabels("postgres").Set(1);

    await using var command = new NpgsqlCommand(
        """
        SELECT id, event_type, task_id, task_title, message, created_at
        FROM activity_events
        ORDER BY created_at DESC, id DESC
        LIMIT 20;
        """,
        connection
    );

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        events.Add(ReadActivityEvent(reader));
    }

    logger.LogInformation("Activity list requested. Returned {ActivityCount} events", events.Count);

    return Results.Ok(events);
});

app.MapPost("/internal/activity", async (CreateActivityEventRequest request) =>
{
    var validationErrors = ValidateActivityEvent(request);

    if (validationErrors.Count > 0)
    {
        logger.LogWarning("Activity event validation failed: {ValidationDetails}", string.Join("; ", validationErrors));
        return Results.BadRequest(new ValidationErrorResponse("Validation failed", validationErrors));
    }

    await using var connection = new NpgsqlConnection(GetConnectionString());
    await connection.OpenAsync();
    dependencyUp.WithLabels("postgres").Set(1);

    await using var command = new NpgsqlCommand(
        """
        INSERT INTO activity_events (event_type, task_id, task_title, message)
        VALUES (@event_type, @task_id, @task_title, @message)
        RETURNING id, event_type, task_id, task_title, message, created_at;
        """,
        connection
    );

    command.Parameters.AddWithValue("event_type", request.EventType.Trim());
    command.Parameters.AddWithValue("task_id", request.TaskId is null ? DBNull.Value : request.TaskId);
    command.Parameters.AddWithValue("task_title", request.TaskTitle.Trim());
    command.Parameters.AddWithValue("message", request.Message.Trim());

    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();

    var activityEvent = ReadActivityEvent(reader);
    activityEventsRecorded.WithLabels(activityEvent.EventType).Inc();

    logger.LogInformation(
        "Activity event recorded. Event ID {ActivityEventId}, Type {ActivityEventType}, Task ID {TaskId}",
        activityEvent.Id,
        activityEvent.EventType,
        activityEvent.TaskId
    );

    return Results.Created($"/api/activity/{activityEvent.Id}", activityEvent);
});

app.Run();

record ActivityEvent(
    int Id,
    string EventType,
    int? TaskId,
    string TaskTitle,
    string Message,
    DateTime CreatedAt
);

record CreateActivityEventRequest(
    string EventType,
    int? TaskId,
    string TaskTitle,
    string Message
);

record ValidationErrorResponse(
    string Error,
    IReadOnlyList<string> Details
);
