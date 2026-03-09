using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class TableDataValidationService
    {
        private const string ValidationStep = OperationSteps.Validation.Data;

        public static OperationResult ValidateTableTypesAndValuesDetailed(TableData tableData, AvailableTypes availableTypes)
        {
            OperationResult validationResult = new OperationResult(OperationResultIdentifiers.Validator);
            ValidateTableTypesAndValues(tableData, availableTypes, validationResult);
            return validationResult;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes, OperationResult? validationResult = null)
        {
            OperationResult result = validationResult ?? new OperationResult(OperationResultIdentifiers.Validator);

            if (tableData is ValueTableData valueTableData)
            {
                ValueTableDataValidationService.Validate(valueTableData, availableTypes, result);
                return result.IsSuccess;
            }

            if (tableData is DatabaseTableData databaseTableData)
            {
                DatabaseTableDataValidationService.Validate(databaseTableData, availableTypes, result);
                return result.IsSuccess;
            }

            if (tableData is ConstantTableData constantTableData)
            {
                ConstantTableDataValidationService.Validate(constantTableData, result);
                return result.IsSuccess;
            }

            result.AddError("Tried to validate an unsupported table type.", step: ValidationStep);

            return result.IsSuccess;
        }
    }
}
