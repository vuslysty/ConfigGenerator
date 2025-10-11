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
        // TODO Maybe we do not need type and TryGetItemWithId, only TryGetItemWithTableStringId
        Type Type { get; }
        bool TryGetItemWithId(object key, out object value);
        bool TryGetItemWithTableStringId(string id, out object value);
    }
}