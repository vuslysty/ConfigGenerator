// See https://aka.ms/new-console-template for more information
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ConfigGenerator;
using ConfigGenerator.ConfigInfrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using CodeGenerator = ConfigGenerator.ConfigInfrastructure.CodeGenerator;
using Configs = YourNamespace.Configs;

await LoadGoogleCredentials();

async Task LoadGoogleCredentials()
{
    string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
    string ApplicationName = "Google Sheets API Example";
    string spreadsheetId = "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
    string credentialsFile = "credentials.json";

    // Завантажуємо облікові дані
    GoogleCredential credential;
    using (var stream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read))
    {
        credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
    }

    // Створюємо службу Google Sheets
    var service = new SheetsService(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName,
    });

    // Отримуємо метадані таблиці
    var spreadsheetRequest = service.Spreadsheets.Get(spreadsheetId);
    Spreadsheet spreadsheet = await spreadsheetRequest.ExecuteAsync();

    // Формуємо діапазони для всіх листів
    List<string> ranges = new List<string>();
    foreach (var sheet in spreadsheet.Sheets)
    {
        string sheetName = sheet.Properties.Title;
        ranges.Add(sheetName); // Додаємо діапазон для кожного листа
    }

    // Використовуємо batchGet для отримання даних з усіх листів
    var batchGetRequest = service.Spreadsheets.Values.BatchGet(spreadsheetId);
    batchGetRequest.Ranges = ranges;
    BatchGetValuesResponse response = await batchGetRequest.ExecuteAsync();

    CultureInfo.CurrentCulture = new CultureInfo("en-US");
    
    var allTables = new List<TableData>();
    
    // Обробка отриманих даних
    foreach (var valueRange in response.ValueRanges)
    {
        var tableName = valueRange.Range.Split('!')[0];
        
        if (TableDataUtilities.ExtractTablesFromPage(tableName, valueRange.Values, out List<TableData> resultTables))
        {
            allTables.AddRange(resultTables);
        }
    }
    
    AvailableTypes availableTypes = new AvailableTypes();
    availableTypes.RegisterDefaultTypes();

    foreach (var tableData in allTables)
    {
        switch (tableData)
        {
            case DatabaseTableData databaseTableData:
                if (databaseTableData.IdType == AvailableTypes.String.TypeName)
                {
                    availableTypes.Register(new DatabaseTableTypeDescriptor(databaseTableData));
                }
                break;
        }
    }

    foreach (var tableData in allTables)
    {
        TableDataUtilities.ValidateTableTypesAndValues(tableData, availableTypes);
    }

    string json = TableDataSerializer.Serialize(allTables);
    List<TableData> deserializeObject = TableDataSerializer.Deserialize(json);
    
    Configs configs = new Configs();
    configs.Initialize(deserializeObject);
    
    var code = CodeGenerator.GenerateConfigClasses(allTables);
    
    // 🛠 Отримуємо шлях до папки з `Main()`
    string projectDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
        .Parent.Parent.Parent.FullName;

    // 🗂 Створюємо шлях до підпапки `Generated`
    string generatedFolder = Path.Combine(projectDirectory, "Generated");

    // 📝 Формуємо шлях до файлу
    string filePath = Path.Combine(generatedFolder, "Configs.cs");

    // 📂 Створюємо папку, якщо її ще немає
    Directory.CreateDirectory(generatedFolder);

    // ✍️ Записуємо код у файл
    File.WriteAllText(filePath, code);

    Console.WriteLine($"✅ Файл збережено: {filePath}");
}