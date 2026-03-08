using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.Application.Reporting;

public sealed class PipelineRunReporter
{

    public List<string> BuildParsingLines(List<TableData> tables)
    {
        List<string> lines = new List<string>
        {
            $"Parsed tables: {tables.Count}",
        };

        foreach (TableData table in tables)
        {
            string tableType = table switch
            {
                ValueTableData => "value",
                DatabaseTableData => "database",
                ConstantTableData => "constant",
                _ => "unknown",
            };

            lines.Add($"- {table.Name} ({tableType}) [{table.StartRow},{table.StartCol}]..[{table.EndRow},{table.EndCol}]");
        }

        return lines;
    }

    public List<string> BuildValidationLines(ParsedTablesResult parsed)
    {
        return BuildValidationLines(new PipelineRunResult(parsed, new GenerationResult()));
    }

    public List<string> BuildValidationLines(PipelineRunResult runResult)
    {
        List<string> lines = new List<string>();

        foreach (ValidationIssue issue in runResult.ParsedTablesResult.ValidationResult.Issues)
        {
            string tablePart = string.IsNullOrWhiteSpace(issue.TableName) ? string.Empty : $" Table={issue.TableName};";
            string rowPart = issue.Row.HasValue ? $" Row={issue.Row.Value};" : string.Empty;
            string colPart = string.IsNullOrWhiteSpace(issue.Column) ? string.Empty : $" Col={issue.Column};";
            lines.Add($"[{issue.Severity}] {issue.Message}{tablePart}{rowPart}{colPart}");
        }

        return lines;
    }

    public List<string> BuildGenerationLines(PipelineRunResult runResult)
    {
        List<string> lines = new List<string>();

        foreach (GenerationMessage message in runResult.GenerationResult.Messages)
        {
            string outputPart = string.IsNullOrWhiteSpace(message.OutputPath) ? string.Empty : $" Output={message.OutputPath};";
            lines.Add($"[{message.Severity}] [{message.Step}] {message.Message}{outputPart}");
        }

        return lines;
    }
}
