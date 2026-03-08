using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils
{
    public static class ConstantValueAssignmentService
    {
        public static void AssignValues(ConstantTableData constantTableData)
        {
            int intId = 0;
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

            for (int i = 0; i < constantTableData.Items.Count; i++)
            {
                ConstantTableDataItem item = constantTableData.Items[i];

                if (string.IsNullOrWhiteSpace(item.StringValue))
                {
                    int validId = GetNextValidId();
                    item.Value = validId;
                    idToIndexMap.Add(validId, i);
                    intId = validId + 1;
                    continue;
                }

                if (AvailableTypes.Int.Parse(item.StringValue, out var parsedId))
                {
                    int id = (int)parsedId;
                    if (idToIndexMap.TryGetValue(id, out int index))
                    {
                        idToIndexMap[id] = i;
                        int validId = GetNextValidId();

                        ConstantTableDataItem otherItem = constantTableData.Items[i];
                        otherItem.Value = validId;

                        idToIndexMap.Add(validId, index);
                        intId = validId + 1;
                    }
                    else
                    {
                        item.Value = id;
                        idToIndexMap.Add(id, i);
                    }
                }
                else
                {
                    int validId = GetNextValidId();
                    item.Value = validId;
                    idToIndexMap.Add(validId, i);
                    intId = validId + 1;
                }
            }
        }
    }
}
