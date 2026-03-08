using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;

namespace ConfigGenerator.Application;

public interface ITypeRegistryFactory
{
    AvailableTypes CreateForTables(List<TableData> tables);
}

public sealed class TypeRegistryFactory : ITypeRegistryFactory
{
    public AvailableTypes CreateForTables(List<TableData> tables)
    {
        AvailableTypes availableTypes = new AvailableTypes();
        availableTypes.RegisterDefaultTypes();

        foreach (TableData tableData in tables)
        {
            switch (tableData)
            {
                case DatabaseTableData databaseTableData:
                    availableTypes.Register(new DatabaseTableTypeDescriptor(databaseTableData));
                    break;

                case ConstantTableData constantTableData:
                    availableTypes.Register(new ConstantTableTypeDescriptor(constantTableData));
                    break;
            }
        }

        return availableTypes;
    }
}
