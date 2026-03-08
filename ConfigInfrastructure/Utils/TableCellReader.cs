using System.Collections.Generic;

namespace ConfigGenerator.ConfigInfrastructure.Utils;

public static class TableCellReader
{
    public static bool TryGetCellData(IList<IList<object>> pageData, int row, int col, out string cellData)
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

    public static string? GetCellData(IList<IList<object>> pageData, int row, int col)
    {
        if (TryGetCellData(pageData, row, col, out string cellData))
        {
            return cellData;
        }

        return null;
    }
}
