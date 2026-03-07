using System;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public sealed class ConstantTableCandidateParser : ITableCandidateParser
{
    public bool TryParse(TableParsingContext context, out TableData? tableData)
    {
        tableData = null;

        if (context.PrimaryKey != PrimaryKey.Const)
        {
            return false;
        }

        string? possibleValueStr = TableCellReader.GetCellData(context.PageData, context.StartRow, context.StartCol + 1)?.Trim();
        if (!string.Equals(possibleValueStr, "value", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        tableData = ConstantTableExtractor.Parse(context.StartRow, context.StartCol, context.TableName, context.PageData);
        return true;
    }
}
