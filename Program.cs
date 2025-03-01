// See https://aka.ms/new-console-template for more information
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ConfigGenerator;
using ConfigGenerator.ConfigInfrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


// Створення об'єкту CodeCompileUnit
var compileUnit = new CodeCompileUnit();

// Створення простору імен
var namespaceDeclaration = new CodeNamespace("MyNamespace");

// Створення класу
var classDeclaration = new CodeTypeDeclaration("MyClass");
classDeclaration.IsClass = true;

// Додавання поля до класу
var field = new CodeMemberField(typeof(string), "myField");
classDeclaration.Members.Add(field);

// Додавання властивості до класу
var property = new CodeMemberProperty();
property.Name = "MyProperty";
property.Type = new CodeTypeReference(typeof(string));
property.Attributes = MemberAttributes.Public | MemberAttributes.Final;
property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "myField")));
property.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "myField"), new CodePropertySetValueReferenceExpression()));
classDeclaration.Members.Add(property);

// Додавання класу до простору імен
namespaceDeclaration.Types.Add(classDeclaration);
compileUnit.Namespaces.Add(namespaceDeclaration);

// Створення генератора коду
var provider = CodeDomProvider.CreateProvider("CSharp");

// Визначення параметрів генерації
var options = new CodeGeneratorOptions();
options.BracingStyle = "C";

// Генерація коду та запис у файл
using (var writer = new StringWriter())
{
    provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);
    Console.WriteLine(writer.ToString());
}

await LoadGoogleCredentials();

static async Task LoadGoogleCredentials()
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
    
    Console.WriteLine(json);
}
