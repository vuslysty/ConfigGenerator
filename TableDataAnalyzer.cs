using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConfigGenerator
{
    public class TableData
    {

    }

    public struct PossibleTable
    {
        public string name;
        public int row;
        public int col;
    }

    public class TableDataAnalyzer
    {
        static string _tableStartPattern = @"^#([A-Za-z][A-Za-z0-9_ ]*)$";

        private static List<PossibleTable> GetPossibleTables(string pageName, IList<IList<object>> pageData)
        {
            List<PossibleTable> possibleTables = new List<PossibleTable>();
            int rowCount = pageData.Count;

            for (int row = 0; row < pageData.Count; row++)
            {
                int columnCount = pageData[row].Count;

                for (int col = 0; col < columnCount; col++)
                {
                    string cellData = (string)pageData[row][col];

                    if (row == 0 && col == 0 && cellData == "id")
                    {
                        possibleTables.Add(new PossibleTable()
                        {
                            name = pageName, row = row, col = col
                        });
                    }
                    else if (cellData.StartsWith('#') && Regex.IsMatch(cellData, _tableStartPattern))
                    {
                        string tableName = cellData.Substring(1).Trim();
                        int nextRowId = row + 1;

                        // Data in the cell below the table name exists
                        if (nextRowId < rowCount && col < pageData[nextRowId].Count)
                        {
                            string nextCellData = (string)pageData[nextRowId][col];

                            if (nextCellData == "id")
                            {
                                possibleTables.Add(new PossibleTable()
                                {
                                    name = tableName, row = nextRowId, col = col
                                });
                            }
                        }
                    }
                }
            }

            return possibleTables;
        }

        public static List<PossibleTable> ExtractData(string pageName, IList<IList<object>> pageData)
        {
            var possibleTables = GetPossibleTables(pageName, pageData);
            return possibleTables;
        }
    }
}
