using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

internal static class ConstantTableDataValidationService
{
    private const string ValidationStep = OperationSteps.Validation.ConstantData;

    public static void Validate(ConstantTableData constantTableData, OperationResult result)
    {
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
                    ColumnIndexFormatter.ToColumnName(constantTableData.StartCol + 1),
                    step: ValidationStep);
            }
        }
    }
}
