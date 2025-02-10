// See https://aka.ms/new-console-template for more information
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConfigGenerator;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

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

    await LoadGoogleCredentials();
}

static async Task LoadGoogleCredentials()
{
    string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
    string ApplicationName = "Google Sheets API Example";
    string spreadsheetId = "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
    string credentialsFile = "test-project-416421-162125dc3989.json";

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

    // Обробка отриманих даних
    foreach (var valueRange in response.ValueRanges)
    {
        var tableName = valueRange.Range.Split('!')[0];
        var tables = TableDataAnalyzer.ExtractTablesFromPage(tableName, valueRange.Values);

        // Console.WriteLine($"Дані з листа '{valueRange.Range}':");
        // foreach (var row in valueRange.Values)
        // {
        //     Console.WriteLine(string.Join(", ", row));
        // }
    }
}
