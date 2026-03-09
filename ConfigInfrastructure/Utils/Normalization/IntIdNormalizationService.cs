using System.Collections.Generic;
using ConfigGenerator.ConfigInfrastructure.Data;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class IntIdNormalizationService
{
    public static void NormalizeConstantValues(ConstantTableData constantTableData)
    {
        List<string?> rawValues = new(constantTableData.Items.Count);

        foreach (ConstantTableDataItem item in constantTableData.Items)
        {
            rawValues.Add(item.StringValue);
        }

        int[] normalizedIds = NormalizeIds(rawValues, startId: 0, autoAssignInvalidValues: true);

        for (int i = 0; i < normalizedIds.Length; i++)
        {
            constantTableData.Items[i].Value = normalizedIds[i];
        }
    }

    public static void NormalizeDatabaseIds(DatabaseTableData tableData)
    {
        List<string?> rawValues = new(tableData.DataObjects.Count);

        foreach (DataObject item in tableData.DataObjects)
        {
            DataField idField = item.Fields[0];

            if (idField.Values.Count == 0)
            {
                idField.Values.Add(string.Empty);
            }

            rawValues.Add(idField.Values[0]);
        }

        int?[] normalizedIds = NormalizeIdsKeepingInvalidValues(rawValues, startId: 1);

        for (int i = 0; i < normalizedIds.Length; i++)
        {
            if (!normalizedIds[i].HasValue)
            {
                continue;
            }

            DataObject item = tableData.DataObjects[i];
            DataField idField = item.Fields[0];
            idField.Values[0] = normalizedIds[i]!.Value.ToString();
        }
    }

    private static int[] NormalizeIds(IReadOnlyList<string?> rawValues, int startId, bool autoAssignInvalidValues)
    {
        int nextCandidateId = startId;
        int[] normalizedIds = new int[rawValues.Count];
        Dictionary<int, int> idToIndexMap = new();

        int GetNextValidId()
        {
            while (idToIndexMap.ContainsKey(nextCandidateId))
            {
                nextCandidateId++;
            }

            return nextCandidateId;
        }

        for (int i = 0; i < rawValues.Count; i++)
        {
            string? rawValue = rawValues[i];

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                int assignedId = GetNextValidId();
                normalizedIds[i] = assignedId;
                idToIndexMap[assignedId] = i;
                nextCandidateId = assignedId + 1;
                continue;
            }

            if (!AvailableTypes.Int.Parse(rawValue, out var parsedId))
            {
                if (!autoAssignInvalidValues)
                {
                    continue;
                }

                int assignedId = GetNextValidId();
                normalizedIds[i] = assignedId;
                idToIndexMap[assignedId] = i;
                nextCandidateId = assignedId + 1;
                continue;
            }

            int id = (int)parsedId;

            if (idToIndexMap.TryGetValue(id, out int duplicatedIndex))
            {
                int reassignedId = GetNextValidId();
                normalizedIds[duplicatedIndex] = reassignedId;
                idToIndexMap[reassignedId] = duplicatedIndex;
                nextCandidateId = reassignedId + 1;
            }

            normalizedIds[i] = id;
            idToIndexMap[id] = i;
        }

        return normalizedIds;
    }

    private static int?[] NormalizeIdsKeepingInvalidValues(IReadOnlyList<string?> rawValues, int startId)
    {
        int[] normalizedIds = NormalizeIds(rawValues, startId, autoAssignInvalidValues: false);
        int?[] result = new int?[rawValues.Count];

        for (int i = 0; i < rawValues.Count; i++)
        {
            string? value = rawValues[i];

            if (string.IsNullOrWhiteSpace(value))
            {
                result[i] = normalizedIds[i];
                continue;
            }

            if (AvailableTypes.Int.Parse(value, out _))
            {
                result[i] = normalizedIds[i];
            }
        }

        return result;
    }
}
