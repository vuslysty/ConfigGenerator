using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure
{
    public interface ITableDataSerializer
    {
        string Serialize(List<TableData> tables);
        List<TableData> Deserialize(string json);
    }
}