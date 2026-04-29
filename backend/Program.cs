var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disable HTTPS redirection for now because Docker/Nginx will handle routing later.
// app.UseHttpsRedirection();

var tasks = new List<TaskItem>
{
    new TaskItem(
        1,
        "Set up LocalDeploy Lab",
        "Create the first backend API endpoints",
        "In Progress",
        "High",
        DateTime.UtcNow,
        DateTime.UtcNow
    )
};

var nextId = 2;

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "running",
        service = "localdeploy-api"
    });
});

app.MapGet("/api/tasks", () =>
{
    return Results.Ok(tasks);
});

app.MapGet("/api/tasks/{id:int}", (int id) =>
{
    var task = tasks.FirstOrDefault(task => task.Id == id);

    return task is null
        ? Results.NotFound()
        : Results.Ok(task);
});

app.MapPost("/api/tasks", (CreateTaskRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new
        {
            error = "Task title is required."
        });
    }

    var now = DateTime.UtcNow;

    var task = new TaskItem(
        nextId++,
        request.Title,
        request.Description ?? "",
        request.Status ?? "Pending",
        request.Priority ?? "Medium",
        now,
        now
    );

    tasks.Add(task);

    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPut("/api/tasks/{id:int}", (int id, UpdateTaskRequest request) =>
{
    var index = tasks.FindIndex(task => task.Id == id);

    if (index == -1)
    {
        return Results.NotFound();
    }

    var existingTask = tasks[index];

    var updatedTask = existingTask with
    {
        Title = request.Title ?? existingTask.Title,
        Description = request.Description ?? existingTask.Description,
        Status = request.Status ?? existingTask.Status,
        Priority = request.Priority ?? existingTask.Priority,
        UpdatedAt = DateTime.UtcNow
    };

    tasks[index] = updatedTask;

    return Results.Ok(updatedTask);
});

app.MapDelete("/api/tasks/{id:int}", (int id) =>
{
    var task = tasks.FirstOrDefault(task => task.Id == id);

    if (task is null)
    {
        return Results.NotFound();
    }

    tasks.Remove(task);

    return Results.NoContent();
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
