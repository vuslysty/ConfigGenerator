using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace ConfigGenerator.Spreadsheet;

public class SpreadsheetPageData
{
    public string name;
    public IList<IList<object>> values;
}

public interface ISpreadsheetDataSource
{
    Task<List<SpreadsheetPageData>> GetAllSheetsDataAsync();
}

public class GoogleSheetDataSource : ISpreadsheetDataSource
{
    private readonly string _credentialsFilePath;
    private readonly string _spreadsheetId;
    
    public GoogleSheetDataSource(string credentialsFilePath, string spreadsheetId)
    {
        _credentialsFilePath = credentialsFilePath;
        _spreadsheetId = spreadsheetId;
    }
    
    public async Task<List<SpreadsheetPageData>> GetAllSheetsDataAsync()
    {
        // Завантажуємо облікові дані
        GoogleCredential credential;
        
        string[] scopes = { SheetsService.Scope.SpreadsheetsReadonly };

        await using (var stream = new FileStream(_credentialsFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
        }
        
        // Створюємо службу Google Sheets
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });
        
        // Отримуємо метадані таблиці
        var spreadsheetRequest = service.Spreadsheets.Get(_spreadsheetId);
        var spreadsheet = await spreadsheetRequest.ExecuteAsync();
        
        // Формуємо діапазони для всіх листів
        List<string> ranges = new List<string>();
        foreach (var sheet in spreadsheet.Sheets)
        {
            string sheetName = sheet.Properties.Title;
            ranges.Add(sheetName); // Додаємо діапазон для кожного листа
        }
        
        // Використовуємо batchGet для отримання даних з усіх листів
        var batchGetRequest = service.Spreadsheets.Values.BatchGet(_spreadsheetId);
        batchGetRequest.Ranges = ranges;
        BatchGetValuesResponse response = await batchGetRequest.ExecuteAsync();
        
        List<SpreadsheetPageData> pages = new List<SpreadsheetPageData>();
        
        // Обробка отриманих даних
        foreach (var valueRange in response.ValueRanges)
        {
            SpreadsheetPageData page = new SpreadsheetPageData()
            {
                name = valueRange.Range.Split('!')[0],
                values = valueRange.Values ?? new List<IList<object>>()
            };
            
            pages.Add(page);
        }

        return pages;
    }
}

public class ExcelFileDataSource : ISpreadsheetDataSource
{
    private readonly string _filePath;

    public ExcelFileDataSource(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<SpreadsheetPageData>> GetAllSheetsDataAsync()
    {
        return await Task.Run(() =>
        {
            var pages = new List<SpreadsheetPageData>();

            using (var workbook = new XLWorkbook(_filePath))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var page = new SpreadsheetPageData
                    {
                        name = worksheet.Name,
                        values = new List<IList<object>>()
                    };

                    var range = worksheet.RangeUsed();
                    if (range != null)
                    {
                        foreach (var row in range.Rows())
                        {
                            var rowData = new List<object>();
                            foreach (var cell in row.Cells())
                            {
                                var cellText = cell.GetString();
                                rowData.Add(cellText);
                            }
                            page.values.Add(rowData);
                        }
                    }

                    pages.Add(page);
                }
            }

            return pages;
        });
    }

    // TODO: Hmmm need to check how it works with formulas
    private object GetCellValue(IXLCell cell)
    {
        if (cell.HasFormula)
        {
            return cell.CachedValue;
        }

        return cell.Value;
    }
}