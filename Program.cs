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
using ConfigGenerator.ConfigInfrastructure.TypeDesctiptors;
using ConfigGenerator.Spreadsheet;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using TestNamespace;
//using TestNamespace;
using CodeGenerator = ConfigGenerator.ConfigInfrastructure.CodeGenerator;

// string projectDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
//     .Parent.Parent.Parent.FullName;
//
// string pathToTest = Path.Combine(projectDirectory, "test.txt");
//
// using (FileStream stream = new FileStream(pathToTest, FileMode.OpenOrCreate, FileAccess.Write))
// {
//     stream.SetLength(0);
//
//     using (StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.UTF8, bufferSize: 128 * 1024))
//     {
//         writer.Write("Hello World!");
//     }
// }

await LoadConfigs();

async Task LoadConfigs()
{
    string spreadsheetId = "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
    string credentialsFile = "credentials.json";

    ISpreadsheetDataSource spreadsheetDataSource = new GoogleSheetDataSource(credentialsFile, spreadsheetId);
    List<SpreadsheetPageData> pages = await spreadsheetDataSource.GetAllSheetsDataAsync();
    
    CultureInfo.CurrentCulture = new CultureInfo("en-US");
    
    var allTables = new List<TableData>();
    
    // Обробка отриманих даних
    foreach (var page in pages)
    {
        if (TableDataUtilities.ExtractTablesFromPage(page.name, page.values, out List<TableData> resultTables))
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
                availableTypes.Register(new DatabaseTableTypeDescriptor(databaseTableData));
                break;
        }
    }

    foreach (var tableData in allTables)
    {
        TableDataUtilities.ValidateTableTypesAndValues(tableData, availableTypes);
    }

    string json = TableDataSerializer.Serialize(allTables);
    List<TableData> deserializeObject = TableDataSerializer.Deserialize(json);
    
    MyConfig.Init(deserializeObject);
    
    var code = CodeGenerator.GenerateConfigClasses(allTables, "MyConfig","TestNamespace");
    
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