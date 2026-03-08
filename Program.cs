using System;
using System.Collections.Generic;
using System.IO;
using ConfigGenerator;
using ConfigGenerator.Application;
using ConfigGenerator.Application.Artifacts;
using ConfigGenerator.Application.Reporting;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;
using TestNamespace;

ITableDataSerializer tableDataSerializer = new TableDataSerializer();
var typeRegistryFactory = new TypeRegistryFactory();
ITableParser tableParser = new TableParser();
ITableValidator tableValidator = new TableValidator(typeRegistryFactory);
IArtifactWriter artifactWriter = new FileArtifactWriter();

ConfigGenerator.ConfigGenerator configGenerator = new ConfigGenerator.ConfigGenerator(
    tableDataSerializer,
    Environment.GetEnvironmentVariable("CONFIG_CLASS_NAME") ?? "MyConfig",
    Environment.GetEnvironmentVariable("CONFIG_NAMESPACE") ?? "TestNamespace",
    typeRegistryFactory,
    tableParser,
    artifactWriter);

string spreadsheetId = Environment.GetEnvironmentVariable("SPREADSHEET_ID")
    ?? "1JphtDv8GUoyqib2y1r_FkiF6JdlrCRg_GIxpWv7v-aQ";
string credentialsFile = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_FILE")
    ?? "credentials.json";

ISpreadsheetDataSource spreadsheetDataSource = new GoogleSheetDataSource(credentialsFile, spreadsheetId);

string projectDirectory = Environment.GetEnvironmentVariable("PROJECT_DIRECTORY")
    ?? Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;

string generatedFolder = Environment.GetEnvironmentVariable("GENERATED_FOLDER")
    ?? Path.Combine(projectDirectory, "Generated");

bool printValidation = string.Equals(
    Environment.GetEnvironmentVariable("PRINT_VALIDATION_ISSUES"),
    "true",
    StringComparison.OrdinalIgnoreCase);

bool printGenerationMessages = string.Equals(
    Environment.GetEnvironmentVariable("PRINT_GENERATION_MESSAGES"),
    "true",
    StringComparison.OrdinalIgnoreCase);

var pipeline = new ConfigGenerationPipeline(configGenerator, tableParser, tableValidator);
var reporter = new PipelineRunReporter();
PipelineRunResult runResult = await pipeline.GenerateDetailedAsync(new List<ISpreadsheetDataSource> { spreadsheetDataSource }, generatedFolder);

if (printValidation)
{
    foreach (string line in reporter.BuildValidationLines(runResult))
    {
        Console.WriteLine(line);
    }
}

if (printGenerationMessages)
{
    foreach (string line in reporter.BuildGenerationLines(runResult))
    {
        Console.WriteLine(line);
    }
}

if (runResult.IsSuccess)
{
    MyConfig.Init(runResult.ParsedTablesResult.Tables);
}

Console.WriteLine(runResult.IsSuccess ? "Success" : "Failure");