using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Humanizer;

namespace ConfigGenerator.ConfigInfrastructure
{
    [Serializable]
    public class TableData
    {
        public string Name;
        
        [NonSerialized]
        public int StartRow;
        [NonSerialized]
        public int StartCol;
        
        [NonSerialized]
        public int EndRow;
        [NonSerialized]
        public int EndCol;
    }

    [Serializable]
    public class ValueTableDataItem
    {
        [NonSerialized]
        public int Row;
        public string Id;
        public string Type;
        public string Value;
        public string Comment;
    }
    
    [Serializable]
    public class ValueTableData : TableData
    {
        public List<ValueTableDataItem> DataValues = new();
    }

    [Serializable]
    public class DatabaseTableFieldDescriptorItem
    {
        [NonSerialized]
        public int Col;
        public string FieldName;
        public string TypeName;
        public string Comment;
    }
    
    [Serializable]
    public class DatabaseTableValuesLineData
    {
        [NonSerialized]
        public int Row;
        public string Id;
        public List<string> Values = new();
    }
    
    [Serializable]
    public class DatabaseTableData : TableData
    {
        public string IdType;
        public List<DatabaseTableFieldDescriptorItem> FieldDescriptors = new();
        public List<DatabaseTableValuesLineData> ValueLines = new();
    }

    public static class TableDataUtilities
    {
        private const string TableStartPattern = @"^#([A-Za-z][A-Za-z0-9 ]*)$";

        public static bool ExtractTablesFromPage(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            tableDataList = new();
            
            var possibleTables = GetPossibleTables(pageName, pageData);
            
            foreach (var possibleTable in possibleTables)
            {
                int startRow = possibleTable.row;
                int startCol = possibleTable.col;
                
                string tableName = ExtractTypeName(possibleTable.name);

                if (GetCellData(pageData, startRow, startCol + 1) == "type" &&
                    GetCellData(pageData, startRow, startCol + 2) == "value")
                {
                    TableData valueTableData = GetValueTableData(startRow, startCol, tableName, pageData);
                    tableDataList.Add(valueTableData);
                }
                else
                {
                    TableData databaseTableData = GetDatabaseTableData(startRow, startCol, tableName, pageData);
                    tableDataList.Add(databaseTableData);
                }
            }

            if (!ValidateTablesByOverlapping(tableDataList))
            {
                return false;
            }
            
            if (!ValidateTablesByNamePatterns(tableDataList))
            {
                return false;
            }
            
            if (!ValidateTablesByDuplicatesInNames(tableDataList))
            {
                return false;
            }
            
            return true;
        }

        public static bool ValidateTableTypesAndValues(TableData tableData, AvailableTypes availableTypes)
        {
            bool isValid = true;
            
            switch (tableData)
            {
                case ValueTableData valueTableData:
                    foreach (ValueTableDataItem dataValue in valueTableData.DataValues)
                    {
                        var typeDescriptor = availableTypes.GetTypeDescriptor(dataValue.Type);

                        if (typeDescriptor == null)
                        {
                            isValid = false;
                            Console.WriteLine($"Error: used invalid data type \"{dataValue.Type}\". " +
                                              $"Table: {valueTableData.Name}, " +
                                              $"Row: {dataValue.Row + 1}, " +
                                              $"Col: {IndexToColumn(valueTableData.StartCol + 1)}");
                            
                            continue;
                        }

                        var parsedValue = typeDescriptor.Parse(dataValue.Value);

                        if (parsedValue == null)
                        {
                            isValid = false;
                            Console.WriteLine($"Error: used invalid data value \"{dataValue.Value}\", " +
                                              $"for type: \"{dataValue.Type}\". " +
                                              $"Table: {valueTableData.Name}, " +
                                              $"Row: {dataValue.Row + 1}, " +
                                              $"Col: {IndexToColumn(valueTableData.StartCol + 2)}");
                        }
                    }
                    
                    break;
                
                case DatabaseTableData databaseTableData:
                    var idTypeDescriptor = availableTypes.GetTypeDescriptor(databaseTableData.IdType);

                    if (idTypeDescriptor == null || 
                        (databaseTableData.IdType != "string" && databaseTableData.IdType != "int"))
                    {
                        isValid = false;
                        Console.WriteLine($"Error: used invalid data type \"{databaseTableData.IdType}\" for id. " +
                                          $"Only valid types for id: \"string\" or \"int\". " +
                                          $"Table: {databaseTableData.Name}, " +
                                          $"Row: {databaseTableData.StartRow + 2}, " +
                                          $"Col: {IndexToColumn(databaseTableData.StartCol)}.");
                    }
                    else
                    {
                        // Validate id values
                        foreach (var lineData in databaseTableData.ValueLines)
                        {
                            var parsedValue = idTypeDescriptor.Parse(lineData.Id);

                            if (parsedValue == null)
                            {
                                Console.WriteLine($"Error: used invalid data value \"{lineData.Id}\", " +
                                                  $"for type: \"{idTypeDescriptor.TypeName}\". " +
                                                  $"Table: {databaseTableData.Name}, " +
                                                  $"Row: {lineData.Row + 1}, " +
                                                  $"Col: {IndexToColumn(databaseTableData.StartCol)}.");
                            }
                        }
                    }

                    for (var fieldIndex = 0; fieldIndex < databaseTableData.FieldDescriptors.Count; fieldIndex++)
                    {
                        var fieldDescriptor = databaseTableData.FieldDescriptors[fieldIndex];
                        var typeDescriptor = availableTypes.GetTypeDescriptor(fieldDescriptor.TypeName);

                        if (typeDescriptor == null)
                        {
                            isValid = false;
                            Console.WriteLine($"Error: used invalid data type \"{fieldDescriptor.TypeName}\". " +
                                              $"Table: {databaseTableData.Name}, " +
                                              $"Row: {databaseTableData.StartRow + 2}, " +
                                              $"Col: {IndexToColumn(fieldDescriptor.Col)}.");
                            continue;
                        }

                        foreach (var valuesLine in databaseTableData.ValueLines)
                        {
                            var valueStr = valuesLine.Values[fieldIndex];
                            var parsedValue = typeDescriptor.Parse(valueStr);
                            
                            if (parsedValue == null)
                            {
                                Console.WriteLine($"Error: used invalid data value \"{valueStr}\", " +
                                                  $"for type \"{fieldDescriptor.TypeName}\". " +
                                                  $"Table: {databaseTableData.Name}, " +
                                                  $"Row: {valuesLine.Row + 1}, " +
                                                  $"Col: {IndexToColumn(fieldDescriptor.Col)}.");
                            }
                        }
                    }

                    break;
                default:
                    Console.WriteLine("Error: tried to validate an unsupported table type");
                    isValid = false;
                    break;
            }

            return isValid;
        }

        private static List<(string name, int row, int col)> GetPossibleTables(
            string pageName,
            IList<IList<object>> pageData)
        {
            List<(string name, int row, int col)> possibleTables = new();

            for (int row = 0; row < pageData.Count; row++)
            {
                int columnCount = pageData[row].Count;

                for (int col = 0; col < columnCount; col++)
                {
                    string cellData = (string)pageData[row][col];

                    if (row == 0 && col == 0 && (cellData == "id" || cellData == string.Empty))
                    {
                        if (cellData == "id")
                        {
                            possibleTables.Add((pageName, row, col));
                        }
                        else
                        {
                            if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                            {
                                if (nextCellData == "id")
                                {
                                    possibleTables.Add((pageName, row + 1, col));
                                }
                            }
                        }
                    }
                    else if (cellData.StartsWith('#') && Regex.IsMatch(cellData, TableStartPattern))
                    {
                        string tableName = cellData.Substring(1).Trim();
                        
                        // Data in the cell below the table name exists
                        if (TryGetCellData(pageData, row + 1, col, out string nextCellData))
                        {
                            if (nextCellData == "id")
                            {
                                possibleTables.Add((tableName, row + 1, col));
                            }
                            else if (nextCellData == "")
                            {
                                if (TryGetCellData(pageData, row + 2, col, out nextCellData))
                                {
                                    if (nextCellData == "id")
                                    {
                                        possibleTables.Add((tableName, row + 2, col));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return possibleTables;
        }

        private static bool TryGetCellData(IList<IList<object>> pageData, int row, int col, out string cellData)
        {
            cellData = null;

            try
            {
                cellData = (string)pageData[row][col];
                cellData = cellData.Trim();
            }
            catch
            {
                return false;
            }
            
            return true;
        }

        private static string GetCellData(IList<IList<object>> pageData, int row, int col)
        {
            if (TryGetCellData(pageData, row, col, out string cellData))
            {
                return cellData;
            }
            
            return null;
        }

        private static TableData GetValueTableData(int startRow, int startCol, string name,
            IList<IList<object>> pageData)
        {
            ValueTableData valueTableData = new ValueTableData()
            {
                Name = name,
                StartRow = startRow,
                StartCol = startCol,
            };

            int idCol = startCol;
            int typeCol = startCol + 1;
            int valueCol = startCol + 2;
            int commentCol = startCol + 3;

            int checkDataRow = startRow + 1;

            while (true)
            {
                ValueTableDataItem itemData = new ValueTableDataItem()
                {
                    Row = checkDataRow
                };
                
                itemData.Id = GetCellData(pageData, checkDataRow, idCol);

                if (string.IsNullOrWhiteSpace(itemData.Id))
                {
                    break;
                }

                if (itemData.Id.StartsWith('!'))
                {
                    checkDataRow++;
                    continue;
                }

                itemData.Type = GetCellData(pageData, checkDataRow, typeCol);

                itemData.Type = string.IsNullOrWhiteSpace(itemData.Type) 
                    ? "string" 
                    : ExtractTypeName(itemData.Type);

                itemData.Value = GetCellData(pageData, checkDataRow, valueCol);

                if (string.IsNullOrWhiteSpace(itemData.Value))
                {
                    itemData.Value = String.Empty;
                }

                itemData.Comment = GetCellData(pageData, checkDataRow, commentCol);

                if (string.IsNullOrWhiteSpace(itemData.Comment))
                {
                    itemData.Comment = String.Empty;
                }

                valueTableData.DataValues.Add(itemData);
                checkDataRow++;
            }

            valueTableData.EndCol = valueTableData.StartCol + 2;

            valueTableData.EndRow = valueTableData.DataValues.Count > 0
                ? valueTableData.DataValues[^1].Row
                : valueTableData.StartRow;

            return valueTableData;
        }

        private static TableData GetDatabaseTableData(int startRow, int startCol, string name,
            IList<IList<object>> pageData)
        {
            DatabaseTableData databaseTableData = new DatabaseTableData
            {
                Name = name,
                StartRow = startRow,
                StartCol = startCol,
            };

            int checkCol = startCol + 1;
            while (TryGetCellData(pageData, startRow, checkCol, out string fieldName) && fieldName != "")
            {
                // We skip column if its name starts with sign '!'
                if (fieldName.StartsWith('!'))
                {
                    checkCol++;
                    continue;
                }
                
                fieldName = ExtractFieldName(fieldName);
                
                DatabaseTableFieldDescriptorItem typeItem = new DatabaseTableFieldDescriptorItem()
                {
                    FieldName = fieldName,
                    Col = checkCol
                };

                if (!TryGetCellData(pageData, startRow + 1, checkCol, out string typeName))
                {
                    // When type is empty we decide that type is equal "string"
                    typeName = "string";
                }
                
                typeName = ExtractTypeName(typeName);
                typeItem.TypeName = typeName;

                if (!TryGetCellData(pageData, startRow - 1, checkCol, out string comment))
                {
                    comment = string.Empty;
                }
                
                typeItem.Comment = comment;

                databaseTableData.FieldDescriptors.Add(typeItem);

                checkCol++;
            }

            if (!TryGetCellData(pageData, startRow + 1, startCol, out string idType))
            {
                idType = "int";
            }

            idType = idType.Trim();
            databaseTableData.IdType = idType;

            int checkDataRow = startRow + 2;
            
            while (true)
            {
                int notEmptyDataCounter = 0;

                string id = GetCellData(pageData, checkDataRow, startCol);

                if (string.IsNullOrWhiteSpace(id))
                {
                    id = string.Empty;
                }
                else
                {
                    notEmptyDataCounter++;
                }

                if (id.StartsWith('!'))
                {
                    checkDataRow++;
                    continue;
                }

                DatabaseTableValuesLineData lineData = new DatabaseTableValuesLineData()
                {
                    Id = id,
                    Row = checkDataRow,
                };

                foreach (var dataType in databaseTableData.FieldDescriptors)
                {
                    if (!TryGetCellData(pageData, checkDataRow, dataType.Col, out string dataContent))
                    {
                        dataContent = string.Empty;
                    }

                    if (dataContent != string.Empty)
                    {
                        notEmptyDataCounter++;
                    }

                    lineData.Values.Add(dataContent);
                }

                if (notEmptyDataCounter > 0)
                {
                    checkDataRow++;
                    databaseTableData.ValueLines.Add(lineData);
                }
                else
                {
                    break;
                }
            }

            if (AvailableTypes.Int.TypeName == databaseTableData.IdType)
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

                for (var i = 0; i < databaseTableData.ValueLines.Count; i++)
                {
                    var lineData = databaseTableData.ValueLines[i];
                    
                    if (string.IsNullOrWhiteSpace(lineData.Id))
                    {
                        int validId = GetNextValidId();
                        lineData.Id = validId.ToString();
                        idToIndexMap.Add(validId, i);
                        intId = validId + 1;
                        continue;
                    }

                    var parsedId = AvailableTypes.Int.Parse(lineData.Id);

                    if (parsedId != null)
                    {
                        int id = (int)parsedId;

                        if (idToIndexMap.TryGetValue(id, out int index))
                        {
                            idToIndexMap[id] = i;
                            int validId = GetNextValidId();
                            databaseTableData.ValueLines[index].Id = validId.ToString();
                            idToIndexMap.Add(validId, index);
                            intId = validId + 1;
                        }
                        else
                        {
                            idToIndexMap.Add(id, i);
                        }
                    }
                }
            }

            databaseTableData.EndCol = databaseTableData.FieldDescriptors.Count > 0
                ? databaseTableData.FieldDescriptors[^1].Col
                : databaseTableData.StartCol;

            databaseTableData.EndRow = databaseTableData.ValueLines.Count > 0
                ? databaseTableData.StartRow + databaseTableData.ValueLines.Count + 1
                : databaseTableData.StartRow + 1;

            return databaseTableData;
        }

        private static bool ValidateTablesByDuplicatesInNames(List<TableData> tableDataList)
        {
            bool isValid = true;
            
            foreach (var tableData in tableDataList)
            {
                if (tableData is ValueTableData valueTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    
                    foreach (var data in valueTableData.DataValues)
                    {
                        if (idToRowMap.TryGetValue(data.Id, out var row))
                        {
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{data.Id}\": " +
                                              $"Row [{row + 1} and {data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol)}]");
                            
                            isValid = false;
                        }
                        else
                        {
                            idToRowMap.Add(data.Id, data.Row);
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    
                    for (var i = 0; i < databaseTableData.ValueLines.Count; i++)
                    {
                        var lineData = databaseTableData.ValueLines[i];
                        var id = lineData.Id;
                        
                        if (idToRowMap.TryGetValue(id, out var row))
                        {
                            isValid = false;
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{id}\": " +
                                            $"Row [{row + 1} and {lineData.Id + 1}], " +
                                            $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
                        }
                        else
                        {
                            idToRowMap.Add(id, lineData.Row);
                        }
                    }

                    Dictionary<string, int> nameToColMap = new();
                    
                    foreach (var data in databaseTableData.FieldDescriptors)
                    {
                        if (nameToColMap.TryGetValue(data.FieldName, out var col))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for field name \"{data.FieldName}\": " +
                                              $"Row [{databaseTableData.StartRow + 1}], " +
                                              $"Col [{IndexToColumn(col)} and {IndexToColumn(data.Col)}]");
                        }
                        else
                        {
                            nameToColMap.Add(data.FieldName, data.Col);
                        }
                    }
                }
            }

            return isValid;
        }
        
        private static bool ValidateTablesByNamePatterns(List<TableData> tableDataList)
        {
            bool isValid = true;
            
            foreach (var tableData in tableDataList)
            {
                if (!IsValidTypeName(tableData.Name))
                {
                    isValid = false;

                    Console.WriteLine($"Table \"{tableData.Name}\" has invalid name");
                }
                
                if (tableData is ValueTableData valueTableData)
                {
                    foreach (var data in valueTableData.DataValues)
                    {
                        if (!IsValidFieldName(data.Id))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id \"{data.Id}\": " +
                                              $"Row [{data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol)}]");
                        }
                        
                        if (!IsValidTypeName(data.Type))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for type \"{data.Type}\": " +
                                              $"Row [{data.Row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol + 1)}]");
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    if (!IsValidTypeName(databaseTableData.IdType))
                    {
                        isValid = false;
                        
                        Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id type \"{databaseTableData.IdType}\": " +
                                          $"Row [{databaseTableData.StartRow + 1}], " +
                                          $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
                    }
                    
                    foreach (var fieldDescriptor in databaseTableData.FieldDescriptors)
                    {
                        if (!IsValidTypeName(fieldDescriptor.TypeName))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for type \"{fieldDescriptor.TypeName}\": " +
                                              $"Row [{databaseTableData.StartRow + 2}], " +
                                              $"Col [{IndexToColumn(fieldDescriptor.Col)}]");
                        }

                        if (!IsValidFieldName(fieldDescriptor.FieldName))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for field \"{fieldDescriptor.FieldName}\": " +
                                              $"Row [{databaseTableData.StartRow + 1}], " +
                                              $"Col [{IndexToColumn(fieldDescriptor.Col)}]");
                        }
                    }

                    if (AvailableTypes.String.TypeName == databaseTableData.IdType)
                    {
                        foreach (var lineData in databaseTableData.ValueLines)
                        {
                            if (!IsValidFieldName(lineData.Id))
                            {
                                isValid = false;
                            
                                Console.WriteLine($"Table \"{tableData.Name}\" has invalid name for id \"{lineData.Id}\": " +
                                                  $"Row [{lineData.Row + 1}], " +
                                                  $"Col [{IndexToColumn(tableData.StartCol)}]");
                            }
                        }
                    }
                }
            }

            return isValid;
        }

        public static string IndexToColumn(int number)
        {
            string columnName = "";
            while (number >= 0)
            {
                columnName = (char)('A' + (number % 26)) + columnName;
                number = (number / 26) - 1;
            }
            return columnName;
        }
        
        private static bool ValidateTablesByOverlapping(List<TableData> tableDataList)
        {
            List<(TableData, TableData)> overlappedTables = new();
            
            foreach (var table1 in tableDataList)
            {
                foreach (var table2 in tableDataList)
                {
                    if (table1 == table2)
                    {
                        continue;
                    }
                    
                    if (AreTablesOverlap(table1, table2))
                    {
                        if (!overlappedTables.Contains((table1, table2)) &&
                            !overlappedTables.Contains((table2, table1)))
                        {
                            overlappedTables.Add((table1, table2));
                        }
                    }
                }
            }
            
            foreach (var tables in overlappedTables)
            {
                Console.WriteLine($"Tables \"{tables.Item1.Name}\" and {tables.Item2.Name} overlap.");
            }
            
            return overlappedTables.Count == 0;
        }

        private static bool AreTablesOverlap(TableData table1, TableData table2)
        {
            Rect table1RectWithSafeZone = new Rect(
                new Vector2(table1.StartCol - 1, table1.StartRow - 1), 
                new Vector2(table1.EndCol + 1, table1.EndRow + 1));
            
            Rect table2Rect = new Rect(
                new Vector2(table2.StartCol, table2.StartRow), 
                new Vector2(table2.EndCol, table2.EndRow));

            return table1RectWithSafeZone.Overlaps(table2Rect);
        }

        private static bool IsValidTypeName(string value)
        {
            const string dataTypePattern = @"^([A-Za-z][A-Za-z0-9]*)$";

            if (value.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            return Regex.IsMatch(value, dataTypePattern);
        }
        
        private static bool IsValidFieldName(string value)
        {
            const string fieldNamePattern = @"^([A-Za-z_][A-Za-z0-9_]*)$";
            
            return Regex.IsMatch(value, fieldNamePattern);
        }

        private static string ExtractTypeName(string value)
        {
            string result = value;
            
            if (value.StartsWith('$'))
            {
                result = result.TrimStart('$').Pascalize();
            }
            
            return RemoveWhitespaces(result);
        }
        
        private static string ExtractFieldName(string value)
        {
            string result = value.Pascalize();
            return RemoveWhitespaces(result);
        }

        private static string RemoveWhitespaces(string value)
        {
            string result = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            return result;
        }
    }
}
