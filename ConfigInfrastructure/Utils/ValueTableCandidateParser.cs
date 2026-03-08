using System;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public sealed class ValueTableCandidateParser : ITableCandidateParser
{
    public bool TryParse(TableParsingContext context, out TableData? tableData)
    {
        tableData = null;

        if (context.PrimaryKey != PrimaryKey.Id)
        {
            return false;
        }

        string? possibleTypeStr = TableCellReader.GetCellData(context.PageData, context.StartRow, context.StartCol + 1)?.Trim();
        string? possibleValueStr = TableCellReader.GetCellData(context.PageData, context.StartRow, context.StartCol + 2)?.Trim();

        if (!string.Equals(possibleTypeStr, "type", StringComparison.InvariantCultureIgnoreCase) ||
            !string.Equals(possibleValueStr, "value", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        tableData = ValueTableExtractor.Parse(context.StartRow, context.StartCol, context.TableName, context.PageData);
        return true;
    }
}
