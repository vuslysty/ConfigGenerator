using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ConfigGenerator;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Spreadsheet;
using TestNamespace;

//using TestNamespace;

//MyConfig.Init(deserializeObject);

ITableDataSerializer tableDataSerializer = new TableDataSerializer();
ConfigGenerator.ConfigGenerator configGenerator = new ConfigGenerator.ConfigGenerator(tableDataSerializer, "MyConfig", "TestNamespace");

string spreadsheetId = "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
string credentialsFile = "credentials.json";

ISpreadsheetDataSource spreadsheetDataSource = new GoogleSheetDataSource(credentialsFile, spreadsheetId);
//ISpreadsheetDataSource spreadsheetDataSource = new ExcelFileDataSource("C:/Users/vuslystyi/Downloads/Config.xlsx");

// 🛠 Отримуємо шлях до папки з `Main()`
string projectDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
    .Parent.Parent.Parent.FullName;

// string projectDirectory = "C:\\_project\\ConfigGeneratorUnityProject\\Assets\\Scripts"; // Path to unity project

// 🗂 Створюємо шлях до підпапки `Generated`
string generatedFolder = Path.Combine(projectDirectory, "Generated");

List<TableData> allTables = new List<TableData>();
bool parseResult = await configGenerator.tryParseTables(
    new List<ISpreadsheetDataSource>(){spreadsheetDataSource}, allTables);

if (parseResult) {
    configGenerator.generateCode(allTables, generatedFolder);
    //configGenerator.generateJson(allTables, generatedFolder);
}

MyConfig.Init(allTables);

Debug.Print("adsf");