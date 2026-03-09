using System.Linq;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

internal static class ValueTableDataValidationService
{
    public static void Validate(ValueTableData valueTableData, AvailableTypes availableTypes, ValidationResult result)
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
                    ColumnIndexFormatter.ToColumnName(valueTableData.StartCol + 1));
                continue;
            }

            if (dataValue.ArrayType.IsArray())
            {
                for (int i = 0; i < dataValue.Values.Count; i++)
                {
                    string value = dataValue.Values[i];
                    int valueRow = dataValue.ValuesRows[i];

                    if (!typeDescriptor.Parse(value, out _))
                    {
                        result.AddError(
                            $"Used invalid data value \"{value}\" for type \"{dataValue.Type}\".",
                            valueTableData.Name,
                            valueRow + 1,
                            ColumnIndexFormatter.ToColumnName(valueTableData.StartCol + 2));
                    }
                }

                continue;
            }

            string singleValue = dataValue.Values.Count > 0 ? dataValue.Values.First() : string.Empty;
            int singleValueRow = dataValue.ValuesRows.Count > 0 ? dataValue.ValuesRows.First() : dataValue.Row;

            if (!typeDescriptor.Parse(singleValue, out _))
            {
                result.AddError(
                    $"Used invalid data value \"{singleValue}\" for type \"{dataValue.Type}\".",
                    valueTableData.Name,
                    singleValueRow + 1,
                    ColumnIndexFormatter.ToColumnName(valueTableData.StartCol + 2));
            }
        }
    }
}
