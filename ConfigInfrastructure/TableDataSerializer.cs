using System.Collections.Generic;
using Newtonsoft.Json;

namespace ConfigGenerator.ConfigInfrastructure;

public static class TableDataSerializer
{
    public static string Serialize(List<TableData> tables)
    {
        return JsonConvert.SerializeObject(tables, GetSettings());
    }

    public static List<TableData> Deserialize(string json)
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