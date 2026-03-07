using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class ValueTableExtractor
{
    private static readonly Regex ArrayTypeRegex = new Regex(
        @"^([A-Za-z0-9_\.]+)\[([^\]]*)\]$",
        RegexOptions.Compiled);

    public static bool IsArrayType(string typeName, out string specialDelimiter, out string cleanTypeName)
    {
        specialDelimiter = null;
        cleanTypeName = null;

        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        typeName = typeName.Trim();
        var match = ArrayTypeRegex.Match(typeName);

        if (!match.Success)
            return false;

        cleanTypeName = match.Groups[1].Value.Trim();
        string bracketContent = match.Groups[2].Value;

        if (string.IsNullOrWhiteSpace(bracketContent))
        {
            specialDelimiter = null;
            return true;
        }

        specialDelimiter = bracketContent;
        return true;
    }

    // TODO Shitcode, need to rewrite, current version is hard for understanding
    // TODO Need add ability to also use \n as delimiter by default
    public static string[] Tokenize(string input, string delimiter = null)
    {
        if (string.IsNullOrEmpty(input))
            return Array.Empty<string>();

        if (string.IsNullOrEmpty(delimiter))
            return new[] { input.Trim() };

        var tokens = new List<string>();
        int position = 0;
        bool inQuotes = false;
        var currentToken = new StringBuilder();
        string escapedDelimiter = Regex.Escape(delimiter);

        while (position < input.Length)
        {
            char currentChar = input[position];

            if (currentChar == '\\' && position + 1 < input.Length)
            {
                char nextChar = input[position + 1];
                switch (nextChar)
                {
                    case '\\': currentToken.Append('\\'); break;
                    case '"': currentToken.Append('"'); break;
                    case 'n': currentToken.Append('\n'); break;
                    case 'r': currentToken.Append('\r'); break;
                    case 't': currentToken.Append('\t'); break;
                    case 'b': currentToken.Append('\b'); break;
                    case 'f': currentToken.Append('\f'); break;
                    case '0': currentToken.Append('\0'); break;
                    default: currentToken.Append(nextChar); break;
                }

                position += 2;
                continue;
            }
            else if (currentChar == '"')
            {
                inQuotes = !inQuotes;
                currentToken.Append(currentChar);
            }
            else if (!inQuotes && position + delimiter.Length <= input.Length &&
                     Regex.IsMatch(input.Substring(position, delimiter.Length), "^" + escapedDelimiter + "$"))
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString().Trim());
                    currentToken.Clear();
                }

                position += delimiter.Length - 1;
            }
            else
            {
                currentToken.Append(currentChar);
            }

            position++;
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString().Trim());
        }

        for (int i = 0; i < tokens.Count; i++)
        {
            tokens[i] = ProcessToken(tokens[i]);
        }

        return tokens.ToArray();
    }

    private static string ProcessToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return token;

        if (token.Length >= 2 && token.StartsWith("\"") && token.EndsWith("\""))
        {
            return token.Substring(1, token.Length - 2);
        }

        return token;
    }

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

        bool isArray = IsArrayType(item.Type, out string delimiter, out string cleanTypeName);

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
                    string[] tokens = Tokenize(valueTuple.value, delimiter);
                    item.Values.AddRange(tokens);
                    item.ValuesRows.Add(valueTuple.row);
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
