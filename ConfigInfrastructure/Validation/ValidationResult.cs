using System.Collections.Generic;
using System.Linq;

namespace ConfigGenerator.ConfigInfrastructure.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationIssue> _issues = new List<ValidationIssue>();

    public IReadOnlyList<ValidationIssue> Issues => _issues;
    public bool IsValid => _issues.All(issue => issue.Severity != ValidationSeverity.Error);

    public void Add(ValidationIssue issue)
    {
        _issues.Add(issue);
    }

    public void AddError(string message, string? tableName = null, int? row = null, string? column = null)
    {
        Add(new ValidationIssue(ValidationSeverity.Error, message, tableName, row, column));
    }

    public void AddWarning(string message, string? tableName = null, int? row = null, string? column = null)
    {
        Add(new ValidationIssue(ValidationSeverity.Warning, message, tableName, row, column));
    }

    public void Merge(ValidationResult another)
    {
        _issues.AddRange(another._issues);
    }
}
