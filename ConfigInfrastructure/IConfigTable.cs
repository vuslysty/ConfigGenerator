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
        bool TryGetItemWithTableStringId(string id, out object value);
    }
}