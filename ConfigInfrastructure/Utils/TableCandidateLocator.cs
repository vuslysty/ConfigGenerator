using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public readonly struct TableCandidate
{
    public string Name { get; }
    public int PrimaryKey { get; }
    public int Row { get; }
    public int Col { get; }

    public TableCandidate(string name, int primaryKey, int row, int col)
    {
        Name = name;
        PrimaryKey = primaryKey;
        Row = row;
        Col = col;
    }
}

public static class TableCandidateLocator
{
    private const string TableStartPattern = @"^#([A-Za-z][A-Za-z0-9 ]*)$";

    public static List<TableCandidate> GetPossibleTables(string pageName, IList<IList<object>> pageData)
    {
        List<TableCandidate> possibleTables = new();

        for (int row = 0; row < pageData.Count; row++)
        {
            int columnCount = pageData[row].Count;

            for (int col = 0; col < columnCount; col++)
            {
                string cellData = (string)pageData[row][col];

                if (row == 0 && col == 0 && (string.IsNullOrWhiteSpace(cellData) || PrimaryKey.IsPrimaryKey(cellData, out int primaryKey)))
                {
                    if (PrimaryKey.IsPrimaryKey(cellData, out primaryKey))
                    {
                        possibleTables.Add(new TableCandidate(pageName, primaryKey, row, col));
                    }
                    else if (string.IsNullOrEmpty(cellData))
                    {
                        if (TableCellReader.TryGetCellData(pageData, row + 1, col, out string nextCellData) &&
                            PrimaryKey.IsPrimaryKey(nextCellData, out primaryKey))
                        {
                            possibleTables.Add(new TableCandidate(pageName, primaryKey, row + 1, col));
                        }
                    }
                }
                else if (cellData.StartsWith('#') && Regex.IsMatch(cellData, TableStartPattern))
                {
                    string tableName = cellData.Substring(1).Trim();

                    if (TableCellReader.TryGetCellData(pageData, row + 1, col, out string nextCellData) &&
                        PrimaryKey.IsPrimaryKey(nextCellData, out primaryKey))
                    {
                        possibleTables.Add(new TableCandidate(tableName, primaryKey, row + 1, col));
                    }
                }
            }
        }

        return possibleTables;
    }
}
