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
    
    public class ValueTableData : TableData
    {
        public List<(int row, string id, string type, string value, string comment)> Data = new();
    }
    
    public class DatabaseTableData : TableData
    {
        public string IdType;
        public List<(string name, string type, string comment, int col)> DataTypes = new();
        public List<List<string>> DataValues = new();
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
                string id = GetCellData(pageData, checkDataRow, idCol);

                if (string.IsNullOrWhiteSpace(id))
                {
                    break;
                }

                if (id.StartsWith('!'))
                {
                    checkDataRow++;
                    continue;
                }

                string type = GetCellData(pageData, checkDataRow, typeCol);

                if (string.IsNullOrWhiteSpace(type))
                {
                    type = "string";
                }

                string value = GetCellData(pageData, checkDataRow, valueCol);

                if (string.IsNullOrWhiteSpace(value))
                {
                    value = String.Empty;
                }

                string comment = GetCellData(pageData, checkDataRow, commentCol);

                if (string.IsNullOrWhiteSpace(comment))
                {
                    comment = String.Empty;
                }

                valueTableData.Data.Add((checkDataRow, id, type, value, comment));
                checkDataRow++;
            }

            valueTableData.EndCol = valueTableData.StartCol + 2;

            valueTableData.EndRow = valueTableData.Data.Count > 0
                ? valueTableData.Data[^1].row
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
            while (TryGetCellData(pageData, startRow, checkCol, out string cellData) && cellData != "")
            {
                // We skip column if its name starts with sign '!'
                if (cellData.StartsWith('!'))
                {
                    checkCol++;
                    continue;
                }

                if (!TryGetCellData(pageData, startRow + 1, checkCol, out string type))
                {
                    // When type is empty we decide that type is equal "string"
                    type = "string";
                }

                if (!TryGetCellData(pageData, startRow + -1, checkCol, out string comment))
                {
                    comment = string.Empty;
                }

                databaseTableData.DataTypes.Add((cellData, type, comment, checkCol));

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

                foreach (var dataType in databaseTableData.DataTypes)
                {
                    if (!TryGetCellData(pageData, checkDataRow, dataType.col, out string dataContent))
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
                    databaseTableData.DataValues.Add(data);
                }
                else
                {
                    break;
                }
            }

            databaseTableData.EndCol = databaseTableData.DataTypes.Count > 0
                ? databaseTableData.DataTypes[^1].col
                : databaseTableData.StartCol;

            databaseTableData.EndRow = databaseTableData.DataValues.Count > 0
                ? databaseTableData.StartRow + databaseTableData.DataValues.Count + 1
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
                    
                    foreach (var data in valueTableData.Data)
                    {
                        if (idToRowMap.TryGetValue(data.id, out var row))
                        {
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for id \"{data.id}\": " +
                                              $"Row [{row + 1} and {data.row + 1}], " +
                                              $"Col [{IndexToColumn(valueTableData.StartCol)}]");
                            
                            isValid = false;
                        }
                        else
                        {
                            idToRowMap.Add(data.id, data.row);
                        }
                    }
                }
                else if (tableData is DatabaseTableData databaseTableData)
                {
                    Dictionary<string, int> idToRowMap = new();
                    bool isIntIdType = databaseTableData.IdType.Equals("int");
                    
                    for (var i = 0; i < databaseTableData.DataValues.Count; i++)
                    {
                        var rowData = databaseTableData.DataValues[i];
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
                    
                    foreach (var data in databaseTableData.DataTypes)
                    {
                        if (nameToColMap.TryGetValue(data.name, out var col))
                        {
                            isValid = false;
                            
                            Console.WriteLine($"Table \"{tableData.Name}\" has duplicates for name \"{data.name}\": " +
                                              $"Row [{databaseTableData.StartRow + 1}], " +
                                              $"Col [{IndexToColumn(col)} and {IndexToColumn(data.col)}]");
                        }
                        else
                        {
                            nameToColMap.Add(data.name, data.col);
                        }
                    }
                }
            }

            return isValid;
        }

        public static string IndexToColumn(int number)
        {
            string columnName = "";
            while (number >= 0)  // Тепер працюємо з 0-based індексом
            {
                columnName = (char)('A' + (number % 26)) + columnName;
                number = (number / 26) - 1; // Віднімаємо 1 після ділення
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
