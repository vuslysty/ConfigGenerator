using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using Newtonsoft.Json;

namespace ConfigGenerator
{
    public class TableDataSerializer : ITableDataSerializer
    {
        public string Serialize(List<TableData> tables)
        {
            return JsonConvert.SerializeObject(tables, GetSettings());
        }

        public List<TableData> Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<List<TableData>>(json, GetSettings());
        }
    
        private static JsonSerializerSettings GetSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            };
        }
    }
}