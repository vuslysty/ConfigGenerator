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

        public static List<TableData> ExtractTablesFromPage(string pageName, IList<IList<object>> pageData)
        {
            var possibleTables = GetPossibleTables(pageName, pageData);

            List<TableData> tableDataList = new();
            
            foreach (var possibleTable in possibleTables)
            {
                int startRow = possibleTable.row;
                int startCol = possibleTable.col;

                if (GetCellData(pageData, startRow, startCol + 1) == "type" &&
                    GetCellData(pageData, startRow, startCol + 2) == "value")
                {
                    ValueTableData valueTableData = new ValueTableData()
                    {
                        Name = possibleTable.name,
                        StartRow = possibleTable.row,
                        StartCol = possibleTable.col,
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
                    
                    tableDataList.Add(valueTableData);
                }
                else
                {
                    DatabaseTableData databaseTableData = new DatabaseTableData
                    {
                        Name = possibleTable.name,
                        StartRow = possibleTable.row,
                        StartCol = possibleTable.col,
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
                    
                    tableDataList.Add(databaseTableData);
                }
            }

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
                        Console.WriteLine($"Tables \"{table1.Name}\" and {table2.Name} overlap.");
                    }
                }
            }

            return tableDataList;
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
