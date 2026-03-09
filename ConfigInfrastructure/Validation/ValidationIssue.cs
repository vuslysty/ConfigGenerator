using ConfigGenerator.Common;

namespace ConfigGenerator.ConfigInfrastructure.Validation;

public sealed class ValidationIssue : IOperationMessage
{
    public MessageSeverity Severity { get; }
    public string Message { get; }
    public string? TableName { get; }
    public int? Row { get; }
    public string? Column { get; }

    public bool IsError => Severity == MessageSeverity.Error;

    public ValidationIssue(
        MessageSeverity severity,
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
