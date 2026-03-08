using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class DatabaseIntIdNormalizationService
    {
        public static void Normalize(DatabaseTableData tableData)
        {
            int intId = 1;
            Dictionary<int, int> idToIndexMap = new();

            int GetNextValidId()
            {
                int id = intId;

                while (idToIndexMap.ContainsKey(id))
                {
                    id++;
                }

                return id;
            }

            for (int i = 0; i < tableData.DataObjects.Count; i++)
            {
                DataObject item = tableData.DataObjects[i];
                DataField idDataField = item.Fields[0];

                string? idValue = null;
                if (idDataField.Values.Count > 0)
                {
                    idValue = idDataField.Values[0];
                }
                else
                {
                    idDataField.Values.Add(string.Empty);
                }

                if (string.IsNullOrWhiteSpace(idValue))
                {
                    int validId = GetNextValidId();
                    idDataField.Values[0] = validId.ToString();
                    idToIndexMap.Add(validId, i);
                    intId = validId + 1;
                    continue;
                }

                if (AvailableTypes.Int.Parse(idValue, out var parsedId))
                {
                    int id = (int)parsedId;
                    if (idToIndexMap.TryGetValue(id, out int index))
                    {
                        idToIndexMap[id] = i;
                        int validId = GetNextValidId();

                        DataObject otherItem = tableData.DataObjects[i];
                        DataField otherItemIdDataField = otherItem.Fields[0];
                        otherItemIdDataField.Values[0] = validId.ToString();

                        idToIndexMap.Add(validId, index);
                        intId = validId + 1;
                    }
                    else
                    {
                        idDataField.Values[0] = id.ToString();
                        idToIndexMap.Add(id, i);
                    }
                }
            }
        }
    }
}
