using System;
using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class ValueTableExtractor
{
    private static bool TryGetValueTableDataItem(int startRow, int startCol, IList<IList<object>> pageData, out ValueTableDataItem item)
    {
        item = new ValueTableDataItem()
        {
            Row = startRow,
        };

        int idCol = startCol;
        int typeCol = startCol + 1;
        int valueCol = startCol + 2;
        int commentCol = startCol + 3;

        int checkRow = startRow;

        string? idData = TableCellReader.GetCellData(pageData, checkRow, idCol);

        if (string.IsNullOrWhiteSpace(idData) || idData.Equals("END"))
        {
            return false;
        }

        item.Id = idData;

        item.Type = TableCellReader.GetCellData(pageData, checkRow, typeCol);

        string valueData = TableCellReader.GetCellData(pageData, checkRow, valueCol);
        List<(int row, string value)> values = new();

        if (!string.IsNullOrWhiteSpace(valueData))
        {
            values.Add((checkRow, valueData));
        }

        string commentData = TableCellReader.GetCellData(pageData, checkRow, commentCol);
        item.Comment = string.IsNullOrWhiteSpace(commentData) ? string.Empty : commentData;

        while (true)
        {
            checkRow++;

            if (checkRow >= pageData.Count)
            {
                break;
            }

            idData = TableCellReader.GetCellData(pageData, checkRow, idCol);

            if (!string.IsNullOrWhiteSpace(idData))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(item.Type))
            {
                item.Type = TableCellReader.GetCellData(pageData, checkRow, typeCol);
            }

            valueData = TableCellReader.GetCellData(pageData, checkRow, valueCol);

            if (!string.IsNullOrWhiteSpace(valueData))
            {
                values.Add((checkRow, valueData));
            }

            commentData = TableCellReader.GetCellData(pageData, checkRow, commentCol);

            if (!string.IsNullOrWhiteSpace(commentData))
            {
                if (item.Comment == string.Empty)
                {
                    item.Comment = commentData;
                }
                else
                {
                    item.Comment += '\n';
                    item.Comment += commentData;
                }
            }
        }

        item.Height = checkRow - startRow;

        if (string.IsNullOrWhiteSpace(item.Type))
        {
            item.Type = AvailableTypes.String.TypeName;
        }

        bool isArray = ValueTypeParsingService.TryParseArrayType(item.Type, out string? delimiter, out string? cleanTypeName);

        if (isArray)
        {
            item.Type = cleanTypeName;

            if (delimiter == null)
            {
                item.ArrayType = ArrayType.Multicell;

                foreach ((int row, string value) valueTuple in values)
                {
                    item.Values.Add(valueTuple.value);
                    item.ValuesRows.Add(valueTuple.row);
                }
            }
            else
            {
                item.ArrayType = ArrayType.OneCell;

                if (values.Count > 0)
                {
                    (int row, string value) valueTuple = values[0];
                    string[] tokens = ValueTypeParsingService.TokenizeArrayValue(valueTuple.value, delimiter);
                    item.Values.AddRange(tokens);

                    for (int i = 0; i < tokens.Length; i++)
                    {
                        item.ValuesRows.Add(valueTuple.row);
                    }
                }
            }
        }
        else
        {
            item.ArrayType = ArrayType.None;

            if (values.Count > 0)
            {
                (int row, string value) valueTuple = values[0];
                item.Values.Add(valueTuple.value);
                item.ValuesRows.Add(valueTuple.row);
            }
        }

        item.Type = TableNameNormalizationService.ExtractTypeName(item.Type);

        return true;
    }

    public static ValueTableData Parse(int startRow, int startCol, string name, IList<IList<object>> pageData)
    {
        ValueTableData valueTableData = new ValueTableData()
        {
            Name = name,
            StartRow = startRow,
            StartCol = startCol,
        };

        int checkRow = startRow + 1;

        while (TryGetValueTableDataItem(checkRow, startCol, pageData, out var dataItem))
        {
            checkRow = dataItem.Row + dataItem.Height;

            if (dataItem.Id.StartsWith('!'))
            {
                continue;
            }

            dataItem.Id = TableNameNormalizationService.ExtractFieldName(dataItem.Id);
            valueTableData.Items.Add(dataItem);
        }

        valueTableData.EndCol = startCol + 3;

        if (valueTableData.Items.Count > 0)
        {
            ValueTableDataItem lastDataValue = valueTableData.Items[^1];
            valueTableData.EndRow = lastDataValue.Row + lastDataValue.Height - 1;
        }
        else
        {
            valueTableData.EndRow = valueTableData.StartRow;
        }

        return valueTableData;
    }
}
