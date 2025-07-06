using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfigGenerator.Spreadsheet;

public interface ISpreadsheetDataSource
{
    Task<List<SpreadsheetPageData>> GetAllSheetsDataAsync();
}