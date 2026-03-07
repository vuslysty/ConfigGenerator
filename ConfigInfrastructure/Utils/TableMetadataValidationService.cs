using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableMetadataValidationService
    {
        public static ValidationResult ValidateTablesMetadataDetailed(List<TableData> tableDataList)
        {
            ValidationResult result = new ValidationResult();
            ValidateTablesByOverlapping(tableDataList, result);
            ValidateTablesByNamePatterns(tableDataList, result);
            ValidateTablesByDuplicatesInNames(tableDataList, result);
            ValidateConstantValueCoercionWarnings(tableDataList, result);
            return result;
        }

        public static bool ValidateTablesByDuplicatesInNames(List<TableData> tableDataList, ValidationResult? validationResult = null)
        {
            ValidationResult result = validationResult ?? new ValidationResult();

            foreach (var tableData in tableDataList)
            {
                if (tableData is ValueTableData valueTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    foreach (var data in valueTableData.Items)
                    {
                        if (idToRowMap.TryGetValue(data.Id, out var row))
                        {
                            result.AddError(
                                $"Duplicate id \"{data.Id}\" found in rows {row + 1} and {data.Row + 1}.",
                                tableData.Name,
                                data.Row + 1,
                                IndexToColumn(valueTableData.StartCol));
                        }
                        else
                        {
                            idToRowMap.Add(data.Id, data.Row);
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    foreach (DataObject dataObject in databaseTableData.DataObjects)
                    {
                        if (dataObject.Fields.Count == 0)
                        {
                            result.AddError(
                                "Data object has no fields while validating duplicate ids.",
                                tableData.Name,
                                dataObject.RowIndex + 1,
                                IndexToColumn(databaseTableData.StartCol));
                            continue;
                        }

                        DataField idDataField = dataObject.Fields[0];

                        if (idDataField.Values.Count == 0)
                        {
                            result.AddError(
                                "Data object has empty id value while validating duplicate ids.",
                                tableData.Name,
                                idDataField.RowIndex + 1,
                                IndexToColumn(databaseTableData.StartCol));
                            continue;
                        }

                        string id = idDataField.Values[0];

                        if (string.IsNullOrWhiteSpace(id))
                        {
                            result.AddError(
                                "Data object has blank id value while validating duplicate ids.",
                                tableData.Name,
                                idDataField.RowIndex + 1,
                                IndexToColumn(databaseTableData.StartCol));
                            continue;
                        }

                        if (idToRowMap.TryGetValue(id, out var row))
                        {
                            result.AddError(
                                $"Duplicate id \"{id}\" found in rows {row + 1} and {idDataField.RowIndex + 1}.",
                                tableData.Name,
                                idDataField.RowIndex + 1,
                                IndexToColumn(databaseTableData.StartCol));
                        }
                        else
                        {
                            idToRowMap.Add(id, idDataField.RowIndex);
                        }
                    }

                    IsValidFieldNodeByNameDuplicates(databaseTableData.RootFieldNode, databaseTableData, result);
                }
                else if (tableData is ConstantTableData constantTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    foreach (var data in constantTableData.Items)
                    {
                        if (idToRowMap.TryGetValue(data.Name, out var row))
                        {
                            result.AddError(
                                $"Duplicate name \"{data.Name}\" found in rows {row + 1} and {data.Row + 1}.",
                                tableData.Name,
                                data.Row + 1,
                                IndexToColumn(constantTableData.StartCol));
                        }
                        else
                        {
                            idToRowMap.Add(data.Name, data.Row);
                        }
                    }
                }
            }

            return result.IsValid;
        }

        private static void IsValidFieldNodeByNameDuplicates(FieldNode fieldNode, DatabaseTableData databaseTableData, ValidationResult result)
        {
            Dictionary<string, FieldNode> fieldNameToFieldNodeMap = new();
            Dictionary<string, FieldNode> fieldInnerTypeToFieldNodeMap = new();

            foreach (FieldNode child in fieldNode.Children)
            {
                if (fieldNameToFieldNodeMap.TryGetValue(child.Name, out FieldNode? duplicateFieldNode))
                {
                    result.AddError(
                        $"Duplicate field name \"{child.Name}\".",
                        databaseTableData.Name,
                        databaseTableData.StartRow + 1,
                        $"{IndexToColumn(duplicateFieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}");
                }
                else
                {
                    fieldNameToFieldNodeMap.Add(child.Name, child);
                }

                if (child.Children.Count == 0)
                {
                    continue;
                }

                if (fieldNode.BaseType == child.BaseType)
                {
                    result.AddError(
                        $"Inner type \"{child.BaseType}\" cannot be the same as outer type.",
                        databaseTableData.Name,
                        databaseTableData.StartRow + 2,
                        $"{IndexToColumn(fieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}");
                }

                if (fieldInnerTypeToFieldNodeMap.TryGetValue(child.BaseType, out duplicateFieldNode))
                {
                    result.AddError(
                        $"Duplicate inner type name \"{child.BaseType}\".",
                        databaseTableData.Name,
                        databaseTableData.StartRow + 2,
                        $"{IndexToColumn(duplicateFieldNode.ColumnIndex)} and {IndexToColumn(child.ColumnIndex)}");
                }
                else
                {
                    fieldInnerTypeToFieldNodeMap.Add(child.BaseType, child);
                }

                IsValidFieldNodeByNameDuplicates(child, databaseTableData, result);
            }
        }

        private static void IsValidFieldNodeByNamePatterns(FieldNode fieldNode, DatabaseTableData databaseTableData, ValidationResult result)
        {
            if (!IsValidTypeName(fieldNode.BaseType))
            {
                result.AddError(
                    $"Invalid type name \"{fieldNode.BaseType}\".",
                    databaseTableData.Name,
                    databaseTableData.StartRow + 1,
                    IndexToColumn(fieldNode.ColumnIndex));
            }

            if (!IsValidFieldName(fieldNode.Name))
            {
                result.AddError(
                    $"Invalid field name \"{fieldNode.Name}\".",
                    databaseTableData.Name,
                    databaseTableData.StartRow + 1,
                    IndexToColumn(fieldNode.ColumnIndex));
            }

            foreach (FieldNode child in fieldNode.Children)
            {
                IsValidFieldNodeByNamePatterns(child, databaseTableData, result);
            }
        }

        public static bool ValidateTablesByNamePatterns(List<TableData> tableDataList, ValidationResult? validationResult = null)
        {
            ValidationResult result = validationResult ?? new ValidationResult();

            foreach (var tableData in tableDataList)
            {
                if (!IsValidTypeName(tableData.Name))
                {
                    result.AddError($"Invalid table name \"{tableData.Name}\".", tableData.Name);
                }

                if (tableData is ValueTableData valueTableData)
                {
                    foreach (var data in valueTableData.Items)
                    {
                        if (!IsValidFieldName(data.Id))
                        {
                            result.AddError(
                                $"Invalid id name \"{data.Id}\".",
                                tableData.Name,
                                data.Row + 1,
                                IndexToColumn(valueTableData.StartCol));
                        }

                        if (!IsValidTypeName(data.Type))
                        {
                            result.AddError(
                                $"Invalid type name \"{data.Type}\".",
                                tableData.Name,
                                data.Row + 1,
                                IndexToColumn(valueTableData.StartCol + 1));
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    foreach (FieldNode childNode in databaseTableData.RootFieldNode.Children)
                    {
                        IsValidFieldNodeByNamePatterns(childNode, databaseTableData, result);
                    }
                }
                else if (tableData is ConstantTableData constantTableData)
                {
                    foreach (var data in constantTableData.Items)
                    {
                        if (!IsValidFieldName(data.Name))
                        {
                            result.AddError(
                                $"Invalid constant name \"{data.Name}\".",
                                tableData.Name,
                                data.Row + 1,
                                IndexToColumn(constantTableData.StartCol));
                        }
                    }
                }
            }

            return result.IsValid;
        }


        private static void ValidateConstantValueCoercionWarnings(List<TableData> tableDataList, ValidationResult result)
        {
            foreach (TableData tableData in tableDataList)
            {
                if (tableData is not ConstantTableData constantTableData)
                {
                    continue;
                }

                foreach (ConstantTableDataItem item in constantTableData.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.StringValue))
                    {
                        continue;
                    }

                    if (!AvailableTypes.Int.Parse(item.StringValue, out _))
                    {
                        result.AddWarning(
                            $"Constant value \"{item.StringValue}\" for \"{item.Name}\" is invalid; fallback auto value \"{item.Value}\" was assigned.",
                            constantTableData.Name,
                            item.Row + 1,
                            IndexToColumn(constantTableData.StartCol + 1));
                    }
                }
            }
        }

        public static bool ValidateTablesByOverlapping(List<TableData> tableDataList, ValidationResult? validationResult = null)
        {
            ValidationResult result = validationResult ?? new ValidationResult();
            List<(TableData, TableData)> overlappedTables = new();

            foreach (var table1 in tableDataList)
            {
                foreach (var table2 in tableDataList)
                {
                    if (table1 == table2)
                    {
                        continue;
                    }

                    if (AreTablesOverlap(table1, table2))
                    {
                        if (!overlappedTables.Contains((table1, table2)) && !overlappedTables.Contains((table2, table1)))
                        {
                            overlappedTables.Add((table1, table2));
                        }
                    }
                }
            }

            foreach (var tables in overlappedTables)
            {
                result.AddError($"Tables \"{tables.Item1.Name}\" and \"{tables.Item2.Name}\" overlap.");
            }

            return result.IsValid;
        }

        private static bool AreTablesOverlap(TableData table1, TableData table2)
        {
            Rect table1RectWithSafeZone = new Rect(
                new Vector2(table1.StartCol - 1, table1.StartRow - 1),
                new Vector2(table1.EndCol + 1, table1.EndRow + 1));

            Rect table2Rect = new Rect(
                new Vector2(table2.StartCol, table2.StartRow),
                new Vector2(table2.EndCol, table2.EndRow));

            return table1RectWithSafeZone.Overlaps(table2Rect);
        }

        private static bool IsValidTypeName(string value)
        {
            const string dataTypePattern = @"^([A-Za-z][A-Za-z0-9]*)(\[\])?$";

            if (value.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Regex.IsMatch(value, dataTypePattern);
        }

        private static bool IsValidFieldName(string value)
        {
            const string fieldNamePattern = @"^([A-Za-z_][A-Za-z0-9_]*)$";
            return Regex.IsMatch(value, fieldNamePattern);
        }

        private static string IndexToColumn(int number)
        {
            string columnName = "";
            while (number >= 0)
            {
                columnName = (char)('A' + (number % 26)) + columnName;
                number = (number / 26) - 1;
            }
            return columnName;
        }
    }
}
