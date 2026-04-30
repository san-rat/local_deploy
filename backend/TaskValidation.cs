public static class TaskValidation
{
    public static readonly IReadOnlyCollection<string> AllowedStatuses =
    [
        "Pending",
        "In Progress",
        "Completed",
        "Blocked"
    ];

    public static readonly IReadOnlyCollection<string> AllowedPriorities =
    [
        "Low",
        "Medium",
        "High",
        "Critical"
    ];

    public static List<string> ValidateCreate(string? title, string? status, string? priority)
    {
        var errors = new List<string>();

        ValidateRequiredTitle(title, errors);
        ValidateAllowedValue("Status", status, AllowedStatuses, errors);
        ValidateAllowedValue("Priority", priority, AllowedPriorities, errors);

        return errors;
    }

    public static List<string> ValidateUpdate(string? title, string? status, string? priority)
    {
        var errors = new List<string>();

        if (title is not null)
        {
            ValidateOptionalTitle(title, errors);
        }

        ValidateAllowedValue("Status", status, AllowedStatuses, errors);
        ValidateAllowedValue("Priority", priority, AllowedPriorities, errors);

        return errors;
    }

    private static void ValidateRequiredTitle(string? title, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title is required.");
            return;
        }

        ValidateTitleLength(title, errors);
    }

    private static void ValidateOptionalTitle(string title, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title cannot be empty when provided.");
            return;
        }

        ValidateTitleLength(title, errors);
    }

    private static void ValidateTitleLength(string title, List<string> errors)
    {
        if (title.Trim().Length > 150)
        {
            errors.Add("Title must be 150 characters or fewer.");
        }
    }

    private static void ValidateAllowedValue(
        string fieldName,
        string? value,
        IReadOnlyCollection<string> allowedValues,
        List<string> errors
    )
    {
        if (value is null)
        {
            return;
        }

        var trimmedValue = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmedValue) || !allowedValues.Contains(trimmedValue))
        {
            errors.Add($"{fieldName} must be one of: {string.Join(", ", allowedValues)}.");
        }
    }
}
