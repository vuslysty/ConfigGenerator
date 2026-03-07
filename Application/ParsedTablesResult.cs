using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.Application;

public sealed class ParsedTablesResult
{
    public List<TableData> Tables { get; }
    public ValidationResult ValidationResult { get; }

    public ParsedTablesResult(List<TableData> tables, ValidationResult validationResult)
    {
        Tables = tables;
        ValidationResult = validationResult;
    }
}
