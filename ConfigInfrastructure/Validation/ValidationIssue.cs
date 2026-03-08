namespace ConfigGenerator.ConfigInfrastructure.Validation;

public enum ValidationSeverity
{
    Error,
    Warning
}

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; }
    public string Message { get; }
    public string? TableName { get; }
    public int? Row { get; }
    public string? Column { get; }

    public ValidationIssue(
        ValidationSeverity severity,
        string message,
        string? tableName = null,
        int? row = null,
        string? column = null)
    {
        Severity = severity;
        Message = message;
        TableName = tableName;
        Row = row;
        Column = column;
    }
}
