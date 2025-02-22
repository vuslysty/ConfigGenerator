using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

namespace ConfigGenerator
{
    public class TableData
    {
        public string Name;
        
        public int StartRow;
        public int StartCol;
        
        public int EndRow;
        public int EndCol;
    }

    public struct ValueTableDataItem
    {
        public int Row;
        public string Id;
        public string Type;
        public string Value;
        public string Comment;
    }
    
    public class ValueTableData : TableData
    {
        public List<ValueTableDataItem> DataValues = new();
    }

    public struct DatabaseTableFieldDescriptorItem
    {
        public int Col;
        public string FieldName;
        public string TypeName;
        public string Comment;
    }
    
    public class DatabaseTableData : TableData
    {
        public string IdType;
        public List<DatabaseTableFieldDescriptorItem> FieldDescriptors = new();
        public List<List<string>> Values = new();
    }

    public class TableDataAnalyzer
    {
        private const string TableStartPattern = @"^#([A-Za-z][A-Za-z0-9_ ]*)$";

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

                if (string.IsNullOrWhiteSpace(itemData.Type))
                {
                    itemData.Type = "string";
                }

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

                typeItem.TypeName = typeName;

                if (!TryGetCellData(pageData, startRow + -1, checkCol, out string comment))
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

            databaseTableData.IdType = idType;

            int checkDataRow = startRow + 2;

            while (true)
            {
                List<string> data = new();
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

                data.Add(id);

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

                    data.Add(dataContent);
                }

                if (notEmptyDataCounter > 0)
                {
                    checkDataRow++;
                    databaseTableData.Values.Add(data);
                }
                else
                {
                    break;
                }
            }

            databaseTableData.EndCol = databaseTableData.FieldDescriptors.Count > 0
                ? databaseTableData.FieldDescriptors[^1].Col
                : databaseTableData.StartCol;

            databaseTableData.EndRow = databaseTableData.Values.Count > 0
                ? databaseTableData.StartRow + databaseTableData.Values.Count + 1
                : databaseTableData.StartRow + 1;

            return databaseTableData;
        }
        
        public static bool ExtractTablesFromPage(string pageName, IList<IList<object>> pageData, out List<TableData> tableDataList)
        {
            tableDataList = new();
            
            var possibleTables = GetPossibleTables(pageName, pageData);
            
            foreach (var possibleTable in possibleTables)
            {
                int startRow = possibleTable.row;
                int startCol = possibleTable.col;

                if (GetCellData(pageData, startRow, startCol + 1) == "type" &&
                    GetCellData(pageData, startRow, startCol + 2) == "value")
                {
                    TableData valueTableData = GetValueTableData(startRow, startCol, possibleTable.name, pageData);
                    tableDataList.Add(valueTableData);
                }
                else
                {
                    TableData databaseTableData = GetDatabaseTableData(startRow, startCol, possibleTable.name, pageData);
                    tableDataList.Add(databaseTableData);
                }
            }

            if (!ValidateTablesByOverlapping(tableDataList))
            {
                return false;
            }
            
            if (!ValidateTablesByDuplicatesInNames(tableDataList))
            {
                return false;
            }
            
            return true;
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
                    bool isIntIdType = databaseTableData.IdType.Equals("int");
                    
                    for (var i = 0; i < databaseTableData.Values.Count; i++)
                    {
                        var rowData = databaseTableData.Values[i];
                        var id = rowData[0];

                        if (isIntIdType && string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        int currentRow = databaseTableData.StartRow + i + 2;
                        
                        if (idToRowMap.TryGetValue(id, out var row))
                        {
                            isValid = false;
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{id}\": " +
                                            $"Row [{row + 1} and {currentRow + 1}], " +
                                            $"Col [{IndexToColumn(databaseTableData.StartCol)}]");
                        }
                        else
                        {
                            idToRowMap.Add(id, currentRow);
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
    }
}
