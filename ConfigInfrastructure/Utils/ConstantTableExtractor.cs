using System;
using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class ConstantTableExtractor
{
    private static bool TryGetConstantTableDataItem(int startRow, int startCol, IList<IList<object>> pageData,
        out ConstantTableDataItem item)
    {
        item = new ConstantTableDataItem()
        {
            Row = startRow,
        };

        int keyCol = startCol;
        int valueCol = startCol + 1;
        int commentCol = startCol + 2;

        string? keyData = TableCellReader.GetCellData(pageData, startRow, keyCol);

        if (string.IsNullOrWhiteSpace(keyData) || keyData.Equals("END"))
        {
            return false;
        }

        item.Name = keyData;
        item.StringValue = TableCellReader.GetCellData(pageData, startRow, valueCol);
        item.Comment = TableCellReader.GetCellData(pageData, startRow, commentCol);

        return true;
    }

    public static ConstantTableData Parse(int startRow, int startCol, string name, IList<IList<object>> pageData)
    {
        ConstantTableData constantTableData = new ConstantTableData()
        {
            Name = name,
            StartRow = startRow,
            StartCol = startCol,
        };

        int checkRow = startRow + 1;

        while (TryGetConstantTableDataItem(checkRow, startCol, pageData, out var dataItem))
        {
            checkRow++;

            if (dataItem.Name.StartsWith('!'))
            {
                continue;
            }

            dataItem.Name = TableNameNormalizationService.ExtractFieldName(dataItem.Name);
            constantTableData.Items.Add(dataItem);
        }

        ConstantValueAssignmentService.AssignValues(constantTableData);

        constantTableData.EndCol = startCol + 2;
        constantTableData.EndRow = constantTableData.Items.Count > 0 ? constantTableData.Items[^1].Row : startRow;

        return constantTableData;
    }
}
