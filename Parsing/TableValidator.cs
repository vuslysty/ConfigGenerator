using System.Collections.Generic;
using ConfigGenerator.Application;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;
using ConfigGenerator.ConfigInfrastructure.Utils;

namespace ConfigGenerator.Parsing;

public interface ITableValidator
{
    OperationResult Validate(List<TableData> tables);
}

public sealed class TableValidator : ITableValidator
{
    private readonly ITypeRegistryFactory _typeRegistryFactory;

    public TableValidator(ITypeRegistryFactory typeRegistryFactory)
    {
        _typeRegistryFactory = typeRegistryFactory;
    }

    public OperationResult Validate(List<TableData> tables)
    {
        OperationResult validationResult = TableMetadataValidationService.ValidateTablesMetadataDetailed(tables);
        var availableTypes = _typeRegistryFactory.CreateForTables(tables);

        foreach (TableData tableData in tables)
        {
            TableDataValidationService.ValidateTableTypesAndValues(tableData, availableTypes, validationResult);
        }

        return validationResult;
    }
}
