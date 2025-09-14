using System;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure
{
    public interface IConfigTable
    {
        void Initialize(TableData tableData, AvailableTypes availableTypes);
        void PostInitialize(TableData tableData, AvailableTypes availableTypes);
    }

    public interface IDatabaseConfigTable : IConfigTable
    {
        Type Type { get; }
        bool TryGetItemWithId(object key, out object value);
    }
}