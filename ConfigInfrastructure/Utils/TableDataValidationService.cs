using System.Linq;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableDataValidationService
    {
        public static ValidationResult ValidateTableTypesAndValuesDetailed(TableData tableData, AvailableTypes availableTypes)
        {
            ValidationResult validationResult = new ValidationResult();
            ValidateTableTypesAndValues(tableData, availableTypes, validationResult);
            return validationResult;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes, ValidationResult? validationResult = null)
        {
            ValidationResult result = validationResult ?? new ValidationResult();

            switch (tableData)
            {
                case ValueTableData valueTableData:
                {
                    foreach (ValueTableDataItem dataValue in valueTableData.Items)
                    {
                        var typeDescriptor = availableTypes.GetTypeDescriptor(dataValue.Type);

                        if (typeDescriptor == null)
                        {
                            result.AddError(
                                $"Used invalid data type \"{dataValue.Type}\".",
                                valueTableData.Name,
                                dataValue.Row + 1,
                                IndexToColumn(valueTableData.StartCol + 1));
                            continue;
                        }

                        if (dataValue.ArrayType.IsArray())
                        {
                            for (var i = 0; i < dataValue.Values.Count; i++)
                            {
                                string value = dataValue.Values[i];
                                int valueRow = dataValue.ValuesRows[i];

                                if (!typeDescriptor.Parse(value, out var parsedValue))
                                {
                                    result.AddError(
                                        $"Used invalid data value \"{value}\" for type \"{dataValue.Type}\".",
                                        valueTableData.Name,
                                        valueRow + 1,
                                        IndexToColumn(valueTableData.StartCol + 2));
                                }
                            }
                        }
                        else
                        {
                            string value = dataValue.Values.Count > 0 ? dataValue.Values.First() : string.Empty;
                            int valueRow = dataValue.ValuesRows.Count > 0 ? dataValue.ValuesRows.First() : dataValue.Row;

                            if (!typeDescriptor.Parse(value, out var parsedValue))
                            {
                                result.AddError(
                                    $"Used invalid data value \"{value}\" for type \"{dataValue.Type}\".",
                                    valueTableData.Name,
                                    valueRow + 1,
                                    IndexToColumn(valueTableData.StartCol + 2));
                            }
                        }
                    }

                    break;
                }

                case DatabaseTableData databaseTableData:
                {
                    foreach (DataObject dataObject in databaseTableData.DataObjects)
                    {
                        IsValidDataObject(dataObject, databaseTableData.RootFieldNode, databaseTableData, availableTypes, result);
                    }

                    break;
                }

                case ConstantTableData:
                    break;

                default:
                    result.AddError("Tried to validate an unsupported table type.");
                    break;
            }

            return result.IsValid;
        }

        private static void IsValidDataObject(
            DataObject dataObject,
            FieldNode baseFieldNode,
            DatabaseTableData databaseTableData,
            AvailableTypes availableTypes,
            ValidationResult result)
        {
            foreach (DataField dataField in dataObject.Fields)
            {
                var currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == dataField.Name);
                var typeDescriptor = availableTypes.GetTypeDescriptor(currentFieldNode.BaseType);

                if (typeDescriptor == null)
                {
                    result.AddError(
                        $"Used invalid data type \"{currentFieldNode.BaseType}\".",
                        databaseTableData.Name,
                        dataField.RowIndex + 1,
                        IndexToColumn(dataField.ColumnIndex));
                    continue;
                }

                if (currentFieldNode.ArrayType.IsArray())
                {
                    for (int i = 0; i < dataField.Values.Count; i++)
                    {
                        string value = dataField.Values[i];
                        int valueRow = dataField.RowIndex;

                        switch (currentFieldNode.ArrayType)
                        {
                            case ArrayType.OneCell:
                                valueRow = dataField.ValuesRows.First();
                                break;
                            case ArrayType.Multicell:
                                valueRow = dataField.ValuesRows[i];
                                break;
                        }

                        if (!typeDescriptor.Parse(value, out var parsedValue))
                        {
                            result.AddError(
                                $"Used invalid data value \"{value}\" for field \"{currentFieldNode.BaseType} {currentFieldNode.Name}\".",
                                databaseTableData.Name,
                                valueRow + 1,
                                IndexToColumn(currentFieldNode.ColumnIndex));
                        }
                    }
                }
                else
                {
                    string value = dataField.Values.Count > 0 ? dataField.Values.First() : string.Empty;
                    int valueRow = dataField.ValuesRows.Count > 0 ? dataField.ValuesRows.First() : dataField.RowIndex;

                    if (!typeDescriptor.Parse(value, out var parsedValue))
                    {
                        result.AddError(
                            $"Used invalid data value \"{value}\" for field \"{currentFieldNode.BaseType} {currentFieldNode.Name}\".",
                            databaseTableData.Name,
                            valueRow + 1,
                            IndexToColumn(currentFieldNode.ColumnIndex));
                    }
                }
            }

            foreach (DataArray array in dataObject.Arrays)
            {
                var currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == array.Name);
                foreach (DataObject dataObjectItem in array.Items)
                {
                    IsValidDataObject(dataObjectItem, currentFieldNode, databaseTableData, availableTypes, result);
                }
            }
        }

        private static string IndexToColumn(int index)
        {
            string column = string.Empty;
            while (index >= 0)
            {
                column = (char)('A' + (index % 26)) + column;
                index = index / 26 - 1;
            }
            return column;
        }
    }
}
