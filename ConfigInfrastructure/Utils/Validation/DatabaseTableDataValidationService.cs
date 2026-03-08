using System.Linq;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

internal static class DatabaseTableDataValidationService
{
    public static void Validate(DatabaseTableData databaseTableData, AvailableTypes availableTypes, ValidationResult result)
    {
        foreach (DataObject dataObject in databaseTableData.DataObjects)
        {
            ValidateDataObject(dataObject, databaseTableData.RootFieldNode, databaseTableData, availableTypes, result);
        }
    }

    private static void ValidateDataObject(
        DataObject dataObject,
        FieldNode baseFieldNode,
        DatabaseTableData databaseTableData,
        AvailableTypes availableTypes,
        ValidationResult result)
    {
        foreach (DataField dataField in dataObject.Fields)
        {
            FieldNode currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == dataField.Name)!;
            var typeDescriptor = availableTypes.GetTypeDescriptor(currentFieldNode.BaseType);

            if (typeDescriptor == null)
            {
                result.AddError(
                    $"Used invalid data type \"{currentFieldNode.BaseType}\".",
                    databaseTableData.Name,
                    dataField.RowIndex + 1,
                    ColumnIndexFormatter.ToColumnName(dataField.ColumnIndex));
                continue;
            }

            if (currentFieldNode.ArrayType.IsArray())
            {
                for (int i = 0; i < dataField.Values.Count; i++)
                {
                    string value = dataField.Values[i];
                    int valueRow = GetArrayValueRow(dataField, currentFieldNode.ArrayType, i);

                    if (!typeDescriptor.Parse(value, out _))
                    {
                        result.AddError(
                            $"Used invalid data value \"{value}\" for field \"{currentFieldNode.BaseType} {currentFieldNode.Name}\".",
                            databaseTableData.Name,
                            valueRow + 1,
                            ColumnIndexFormatter.ToColumnName(currentFieldNode.ColumnIndex));
                    }
                }

                continue;
            }

            string singleValue = dataField.Values.Count > 0 ? dataField.Values.First() : string.Empty;
            int singleValueRow = dataField.ValuesRows.Count > 0 ? dataField.ValuesRows.First() : dataField.RowIndex;

            if (!typeDescriptor.Parse(singleValue, out _))
            {
                result.AddError(
                    $"Used invalid data value \"{singleValue}\" for field \"{currentFieldNode.BaseType} {currentFieldNode.Name}\".",
                    databaseTableData.Name,
                    singleValueRow + 1,
                    ColumnIndexFormatter.ToColumnName(currentFieldNode.ColumnIndex));
            }
        }

        foreach (DataArray array in dataObject.Arrays)
        {
            FieldNode currentFieldNode = baseFieldNode.Children.Find(childFieldNode => childFieldNode.Name == array.Name)!;
            foreach (DataObject item in array.Items)
            {
                ValidateDataObject(item, currentFieldNode, databaseTableData, availableTypes, result);
            }
        }
    }

    private static int GetArrayValueRow(DataField dataField, ArrayType arrayType, int index)
    {
        return arrayType switch
        {
            ArrayType.OneCell => dataField.ValuesRows.First(),
            ArrayType.Multicell => dataField.ValuesRows[index],
            _ => dataField.RowIndex,
        };
    }
}
