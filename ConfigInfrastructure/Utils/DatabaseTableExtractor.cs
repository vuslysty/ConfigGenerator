using System;
using System.Collections.Generic;
using System.Linq;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class DatabaseTableExtractor
{
    public static DatabaseTableData Parse(int startRow, int startCol, string name, IList<IList<object>> pageData)
    {
        var tableData = new DatabaseTableData
        {
            Name = name,
            StartRow = startRow,
            StartCol = startCol,
        };

        var root = new FieldNode()
        {
            Name = "Root",
            ColumnIndex = startCol,
        };

        int startingMaxHeight = int.MaxValue;

        if (!TableCellReader.TryGetCellData(pageData, startRow + 1, startCol, out string idType) || string.IsNullOrWhiteSpace(idType))
        {
            idType = AvailableTypes.Int.TypeName;
            startingMaxHeight = 1;
        }

        idType = TableNameNormalizationService.ExtractTypeName(idType);
        bool isIntTypeId = idType == AvailableTypes.Int.TypeName;

        AddToTree(root, ["id"], idType, startCol, null);

        tableData.RootFieldNode = root;

        int checkCol = startCol + 1;
        tableData.EndCol = startCol;

        while (TableCellReader.TryGetCellData(pageData, startRow, checkCol, out string fieldName))
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                break;
            }

            if (fieldName.StartsWith('!'))
            {
                checkCol++;
                continue;
            }
            
            string[] fieldsPath = fieldName.Split('.');

            for (int i = 0; i < fieldsPath.Length; i++)
            {
                string field = fieldsPath[i];
                fieldsPath[i] = TableNameNormalizationService.ExtractFieldName(field);;
            }

            if (!TableCellReader.TryGetCellData(pageData, startRow + 1, checkCol, out string typeName))
            {
                typeName = AvailableTypes.String.TypeName;
            }

            TableCellReader.TryGetCellData(pageData, startRow - 1, checkCol, out string comment);
            AddToTree(root, fieldsPath, typeName, checkCol, string.IsNullOrWhiteSpace(comment) ? null : comment);
            tableData.EndCol = checkCol;

            checkCol++;
        }

        SortFieldNodes(root);
        SetupBaseTypes(root);

        tableData.DataObjects = new List<DataObject>();
        int row = startRow + 2;

        while (row < pageData.Count)
        {
            string cellData = TableCellReader.GetCellData(pageData, row, startCol);

            if (cellData.Equals("END"))
            {
                break;
            }

            if (!isIntTypeId && string.IsNullOrWhiteSpace(cellData))
            {
                break;
            }

            DataObject obj = ParseObject(row, startingMaxHeight, root, pageData);

            if (!cellData.StartsWith('!'))
            {
                tableData.DataObjects.Add(obj);
            }

            row += obj.Height;
        }

        if (isIntTypeId)
        {
            DatabaseIntIdNormalizationService.Normalize(tableData);
        }

        if (tableData.DataObjects.Count > 0)
        {
            DataObject lastItem = tableData.DataObjects[^1];
            tableData.EndRow = lastItem.RowIndex + lastItem.Height - 1;
        }
        else
        {
            tableData.EndRow = tableData.StartRow + 1;
        }

        return tableData;
    }

    private static DataObject ParseObject(int row, int maxHeight, FieldNode fieldNode, IList<IList<object>> pageData)
    {
        var obj = new DataObject
        {
            RowIndex = row,
            Height = maxHeight,
            ColumnIndex = fieldNode.ColumnIndex
        };

        bool hasHeight = false;
        int checkRow = row;

        foreach (FieldNode child in fieldNode.Children)
        {
            if (child.Children.Count == 0)
            {
                DataField dataField = GetFieldData(checkRow, obj.Height, child, pageData);

                if (!hasHeight)
                {
                    obj.Height = dataField.Height;
                    hasHeight = true;
                }

                obj.Fields.Add(dataField);
            }
            else
            {
                DataArray array = ParseArray(child, checkRow, obj.Height, pageData);
                obj.Arrays.Add(array);
            }
        }

        return obj;
    }

    private static DataArray ParseArray(FieldNode arrayNode, int startRow, int maxHeight, IList<IList<object>> pageData)
    {
        var array = new DataArray
        {
            Name = arrayNode.Name,
            RowIndex = startRow,
            ColumnIndex = arrayNode.ColumnIndex,
            Height = maxHeight,
        };

        int checkRow = startRow;

        while (checkRow < startRow + maxHeight)
        {
            int height = startRow + maxHeight - checkRow;
            DataObject obj = ParseObject(checkRow, height, arrayNode, pageData);

            array.Items.Add(obj);
            checkRow += obj.Height;
        }

        return array;
    }

    private static DataField GetFieldData(int row, int maxHeight, FieldNode field, IList<IList<object>> pageData)
    {
        DataField dataField = new DataField()
        {
            Name = field.Name,
            RowIndex = row,
            ColumnIndex = field.ColumnIndex,
            ValuesRows = new List<int>()
        };

        int height = 0;
        int checkRow = row;

        while (checkRow < pageData.Count)
        {
            if (height >= maxHeight)
            {
                break;
            }

            string value = TableCellReader.GetCellData(pageData, checkRow, field.ColumnIndex);

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (value.Equals("END"))
                {
                    break;
                }

                if (field.ArrayType == ArrayType.Multicell)
                {
                    dataField.Values.Add(value);
                }
                else
                {
                    if (dataField.Values.Count > 0)
                    {
                        break;
                    }

                    if (field.ArrayType == ArrayType.OneCell)
                    {
                        string[] tokens = ValueTableExtractor.Tokenize(value, field.ArrayDelimiter);
                        dataField.Values.AddRange(tokens);
                    }
                    else
                    {
                        dataField.Values.Add(value);
                    }
                }

                dataField.ValuesRows.Add(checkRow);
            }

            checkRow++;
            height++;
        }

        dataField.Height = height;

        return dataField;
    }

    private static void SortFieldNodes(FieldNode fieldNode)
    {
        fieldNode.Children = fieldNode.Children.OrderBy(node => node.Children.Count > 0).ToList();

        foreach (FieldNode child in fieldNode.Children)
        {
            SortFieldNodes(child);
        }
    }

    private static void SetupBaseTypes(FieldNode fieldNode)
    {
        bool hasBaseType = !string.IsNullOrWhiteSpace(fieldNode.BaseType);
        string? childCustomType = null;

        foreach (FieldNode child in fieldNode.Children)
        {
            SetupBaseTypes(child);

            if (!hasBaseType && childCustomType == null)
            {
                childCustomType = child.CustomType;
            }
        }

        if (!hasBaseType)
        {
            fieldNode.BaseType = childCustomType ?? $"{fieldNode.Name}Type";
        }
    }

    private static void AddToTree(FieldNode node, string[] path, string typeDef, int columnIndex, string? comment, int index = 0)
    {
        string current = path[index];
        FieldNode? child = node.Children.FirstOrDefault(c => c.Name == current);

        bool isLastIndex = index == path.Length - 1;

        if (isLastIndex && child != null && child.Children.Count > 0)
        {
            child = null;
        }

        if (child == null)
        {
            child = new FieldNode
            {
                Name = TableNameNormalizationService.ExtractFieldName(current),
                ColumnIndex = columnIndex,
            };

            node.Children.Add(child);
        }

        if (isLastIndex)
        {
            string? customType = null;
            string[] typeParts = typeDef.Split('.', ':');

            if (typeParts.Length > 1)
            {
                customType = TableNameNormalizationService.ExtractTypeName(typeParts[0]);
                typeDef = typeParts[1];
            }

            if (string.IsNullOrWhiteSpace(typeDef))
            {
                typeDef = AvailableTypes.String.TypeName;
            }

            bool isArray = ValueTableExtractor.IsArrayType(typeDef, out string delimiter, out string cleanTypeName);

            if (isArray)
            {
                typeDef = cleanTypeName;

                if (string.IsNullOrWhiteSpace(delimiter))
                {
                    child.ArrayType = ArrayType.Multicell;
                }
                else
                {
                    child.ArrayType = ArrayType.OneCell;
                    child.ArrayDelimiter = delimiter;
                }
            }

            child.BaseType = TableNameNormalizationService.ExtractTypeName(typeDef);
            child.CustomType = customType;
            child.Comment = comment;
            child.ColumnIndex = columnIndex;
        }
        else
        {
            AddToTree(child, path, typeDef, columnIndex, comment, index + 1);
        }
    }
}
