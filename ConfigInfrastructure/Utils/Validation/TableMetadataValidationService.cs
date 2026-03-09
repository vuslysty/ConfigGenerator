using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableMetadataValidationService
    {
        private const string MetadataStep = OperationSteps.Validation.Metadata;

        public static OperationResult ValidateTablesMetadataDetailed(List<TableData> tableDataList)
        {
            OperationResult result = new OperationResult(OperationResultIdentifiers.Validator);
            ValidateTablesByNamePatterns(tableDataList, result);
            ValidateTablesByDuplicatesInNames(tableDataList, result);
            return result;
        }

        public static bool ValidateTablesByDuplicatesInNames(List<TableData> tableDataList, OperationResult? validationResult = null)
        {
            OperationResult result = validationResult ?? new OperationResult(OperationResultIdentifiers.Validator);

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
                                ColumnIndexFormatter.ToColumnName(valueTableData.StartCol), step: MetadataStep);
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
                            throw new InvalidOperationException(
                                $"Regression detected: data object at row {dataObject.RowIndex + 1} in table \"{tableData.Name}\" has no fields.");
                        }

                        DataField idDataField = dataObject.Fields[0];
                        if (idDataField.Values.Count == 0 || string.IsNullOrWhiteSpace(idDataField.Values[0]))
                        {
                            throw new InvalidOperationException(
                                $"Regression detected: empty id at row {idDataField.RowIndex + 1} in table \"{tableData.Name}\".");
                        }

                        string id = idDataField.Values[0];

                        if (idToRowMap.TryGetValue(id, out var row))
                        {
                            result.AddError(
                                $"Duplicate id \"{id}\" found in rows {row + 1} and {idDataField.RowIndex + 1}.",
                                tableData.Name,
                                idDataField.RowIndex + 1,
                                ColumnIndexFormatter.ToColumnName(databaseTableData.StartCol), step: MetadataStep);
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
                                ColumnIndexFormatter.ToColumnName(constantTableData.StartCol), step: MetadataStep);
                        }
                        else
                        {
                            idToRowMap.Add(data.Name, data.Row);
                        }
                    }
                }
            }

            return result.IsSuccess;
        }

        private static void IsValidFieldNodeByNameDuplicates(FieldNode fieldNode, DatabaseTableData databaseTableData, OperationResult result)
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
                        $"{ColumnIndexFormatter.ToColumnName(duplicateFieldNode.ColumnIndex)} and {ColumnIndexFormatter.ToColumnName(child.ColumnIndex)}", step: MetadataStep);
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
                        $"{ColumnIndexFormatter.ToColumnName(fieldNode.ColumnIndex)} and {ColumnIndexFormatter.ToColumnName(child.ColumnIndex)}", step: MetadataStep);
                }

                if (fieldInnerTypeToFieldNodeMap.TryGetValue(child.BaseType, out duplicateFieldNode))
                {
                    result.AddError(
                        $"Duplicate inner type name \"{child.BaseType}\".",
                        databaseTableData.Name,
                        databaseTableData.StartRow + 2,
                        $"{ColumnIndexFormatter.ToColumnName(duplicateFieldNode.ColumnIndex)} and {ColumnIndexFormatter.ToColumnName(child.ColumnIndex)}", step: MetadataStep);
                }
                else
                {
                    fieldInnerTypeToFieldNodeMap.Add(child.BaseType, child);
                }

                IsValidFieldNodeByNameDuplicates(child, databaseTableData, result);
            }
        }

        private static void IsValidFieldNodeByNamePatterns(FieldNode fieldNode, DatabaseTableData databaseTableData, OperationResult result)
        {
            if (!IsValidTypeName(fieldNode.BaseType))
            {
                result.AddError(
                    $"Invalid type name \"{fieldNode.BaseType}\".",
                    databaseTableData.Name,
                    databaseTableData.StartRow + 1,
                    ColumnIndexFormatter.ToColumnName(fieldNode.ColumnIndex), step: MetadataStep);
            }

            if (!IsValidFieldName(fieldNode.Name))
            {
                result.AddError(
                    $"Invalid field name \"{fieldNode.Name}\".",
                    databaseTableData.Name,
                    databaseTableData.StartRow + 1,
                    ColumnIndexFormatter.ToColumnName(fieldNode.ColumnIndex), step: MetadataStep);
            }

            foreach (FieldNode child in fieldNode.Children)
            {
                IsValidFieldNodeByNamePatterns(child, databaseTableData, result);
            }
        }

        public static bool ValidateTablesByNamePatterns(List<TableData> tableDataList, OperationResult? validationResult = null)
        {
            OperationResult result = validationResult ?? new OperationResult(OperationResultIdentifiers.Validator);

            foreach (var tableData in tableDataList)
            {
                if (!IsValidTypeName(tableData.Name))
                {
                    result.AddError($"Invalid table name \"{tableData.Name}\".", tableData.Name, step: MetadataStep);
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
                                ColumnIndexFormatter.ToColumnName(valueTableData.StartCol), step: MetadataStep);
                        }

                        if (!IsValidTypeName(data.Type))
                        {
                            result.AddError(
                                $"Invalid type name \"{data.Type}\".",
                                tableData.Name,
                                data.Row + 1,
                                ColumnIndexFormatter.ToColumnName(valueTableData.StartCol + 1), step: MetadataStep);
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
                                ColumnIndexFormatter.ToColumnName(constantTableData.StartCol), step: MetadataStep);
                        }
                    }
                }
            }

            return result.IsSuccess;
        }
        public static bool ValidateTablesByOverlapping(List<TableData> tableDataList, OperationResult? validationResult = null)
        {
            OperationResult result = validationResult ?? new OperationResult(OperationResultIdentifiers.Validator);
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
                result.AddError($"Tables \"{tables.Item1.Name}\" and \"{tables.Item2.Name}\" overlap.", step: MetadataStep);
            }

            return result.IsSuccess;
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

    }
}
