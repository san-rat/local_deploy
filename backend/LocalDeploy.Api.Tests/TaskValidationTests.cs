using Xunit;

public class TaskValidationTests
{
    [Fact]
    public void ValidateCreate_WithValidInput_ReturnsNoErrors()
    {
        var errors = TaskValidation.ValidateCreate(
            "Write backend tests",
            "Pending",
            "High"
        );

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WithEmptyTitle_ReturnsRequiredTitleError(string title)
    {
        var errors = TaskValidation.ValidateCreate(title, "Pending", "High");

        Assert.Equal(["Title is required."], errors);
    }

    [Fact]
    public void ValidateCreate_WithTitleLongerThan150Characters_ReturnsMaxLengthError()
    {
        var title = new string('A', 151);

        var errors = TaskValidation.ValidateCreate(title, "Pending", "High");

        Assert.Equal(["Title must be 150 characters or fewer."], errors);
    }

    [Fact]
    public void ValidateCreate_WithInvalidStatus_ReturnsAllowedStatusError()
    {
        var errors = TaskValidation.ValidateCreate("Invalid status test", "Done", "High");

        Assert.Equal(
            ["Status must be one of: Pending, In Progress, Completed, Blocked."],
            errors
        );
    }

    [Fact]
    public void ValidateCreate_WithInvalidPriority_ReturnsAllowedPriorityError()
    {
        var errors = TaskValidation.ValidateCreate("Invalid priority test", "Pending", "Urgent");

        Assert.Equal(
            ["Priority must be one of: Low, Medium, High, Critical."],
            errors
        );
    }

    [Fact]
    public void ValidateUpdate_WithOmittedFields_ReturnsNoErrors()
    {
        var errors = TaskValidation.ValidateUpdate(null, null, null);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WithEmptyTitle_ReturnsOptionalTitleError(string title)
    {
        var errors = TaskValidation.ValidateUpdate(title, null, null);

        Assert.Equal(["Title cannot be empty when provided."], errors);
    }

    [Fact]
    public void ValidateUpdate_WithInvalidStatusAndPriority_ReturnsBothErrors()
    {
        var errors = TaskValidation.ValidateUpdate(null, "Done", "Urgent");

        Assert.Equal(
            [
                "Status must be one of: Pending, In Progress, Completed, Blocked.",
                "Priority must be one of: Low, Medium, High, Critical."
            ],
            errors
        );
    }
}
