using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;

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

    public List<string> BuildValidationLines(PipelineRunResult runResult)
    {
        return BuildOperationLines(runResult.ValidationResult);
    }

    public List<string> BuildGenerationLines(PipelineRunResult runResult)
    {
        return BuildOperationLines(runResult.GenerationResult);
    }

    private static List<string> BuildOperationLines(OperationResult result)
    {
        List<string> lines = new List<string>();

        foreach (OperationMessage message in result.Messages)
        {
            string step = string.IsNullOrWhiteSpace(message.Step) ? "general" : message.Step;
            string table = string.IsNullOrWhiteSpace(message.TableName) ? "-" : message.TableName;
            string cell = BuildCell(message.Row, message.Column);
            lines.Add($"[{step}][{result.Identifier}][{message.Severity}] {message.Message} | Table={table}; Cell={cell}");
        }

        return lines;
    }

    private static string BuildCell(int? row, string? column)
    {
        bool hasRow = row.HasValue;
        bool hasColumn = !string.IsNullOrWhiteSpace(column);

        if (!hasRow && !hasColumn)
        {
            return "-";
        }

        if (hasRow && hasColumn)
        {
            return $"{column}{row}";
        }

        return hasColumn ? column! : $"Row {row!.Value}";
    }
}
