using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Utils;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator.Parsing;

public interface ITableParser
{
    Task<List<TableData>> ParseAsync(List<ISpreadsheetDataSource> spreadsheetSources);
}

public sealed class TableParser : ITableParser
{
    public async Task<List<TableData>> ParseAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> allTables = new List<TableData>();
        List<SpreadsheetPageData> allPages = new List<SpreadsheetPageData>();

        foreach (ISpreadsheetDataSource spreadsheetSource in spreadsheetSources)
        {
            List<SpreadsheetPageData> pages = await spreadsheetSource.GetAllSheetsDataAsync();
            allPages.AddRange(pages);
        }

        foreach (SpreadsheetPageData page in allPages)
        {
            if (page.name.StartsWith('\''))
            {
                continue;
            }

            if (TableExtractionService.TryExtractTables(page.name, page.values, out List<TableData> resultTables))
            {
                allTables.AddRange(resultTables);
            }
        }

        return allTables;
    }
}
