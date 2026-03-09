using System.Collections.Generic;
using ConfigGenerator.Common;

namespace ConfigGenerator.ConfigInfrastructure.Validation;

public sealed class ValidationResult : OperationResult<ValidationIssue>
{
    public IReadOnlyList<ValidationIssue> Issues => MessageItems;
    public bool IsValid => IsSuccess;

    public void AddError(string message, string? tableName = null, int? row = null, string? column = null)
    {
        Add(new ValidationIssue(MessageSeverity.Error, message, tableName, row, column));
    }

    public void AddWarning(string message, string? tableName = null, int? row = null, string? column = null)
    {
        Add(new ValidationIssue(MessageSeverity.Warning, message, tableName, row, column));
    }

    public void Merge(ValidationResult another)
    {
        MergeFrom(another);
    }
}
