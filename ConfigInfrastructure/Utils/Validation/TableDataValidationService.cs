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

            if (tableData is ValueTableData valueTableData)
            {
                ValueTableDataValidationService.Validate(valueTableData, availableTypes, result);
                return result.IsValid;
            }

            if (tableData is DatabaseTableData databaseTableData)
            {
                DatabaseTableDataValidationService.Validate(databaseTableData, availableTypes, result);
                return result.IsValid;
            }

            if (tableData is not ConstantTableData)
            {
                result.AddError("Tried to validate an unsupported table type.");
            }

            return result.IsValid;
        }
    }
}
