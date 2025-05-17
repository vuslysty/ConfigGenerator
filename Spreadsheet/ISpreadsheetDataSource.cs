using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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