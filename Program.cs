using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ConfigGenerator;
using ConfigGenerator.Application;
using ConfigGenerator.Application.Artifacts;
using ConfigGenerator.Application.Reporting;
using ConfigGenerator.ConfigInfrastructure;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.Common;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;
using TestNamespace;

GeneratorAppOptions options = GeneratorAppOptions.Load(args);
GeneratorRunMode runMode = options.ResolveRunMode();

ITableDataSerializer tableDataSerializer = new TableDataSerializer();
var typeRegistryFactory = new TypeRegistryFactory();
ITableParser tableParser = new TableParser();
ITableValidator tableValidator = new TableValidator(typeRegistryFactory);
IArtifactWriter artifactWriter = new FileArtifactWriter();

ConfigGenerator.ConfigGenerator configGenerator = new ConfigGenerator.ConfigGenerator(
    tableDataSerializer,
    options.ConfigClassName,
    options.ConfigNamespace,
    typeRegistryFactory,
    tableParser,
    artifactWriter);

var pipeline = new ConfigGenerationPipeline(configGenerator, tableParser, tableValidator);
var reporter = new PipelineRunReporter();

if (!string.IsNullOrWhiteSpace(options.JsonInputFile))
{
    await RunFromJsonAsync(options, runMode, tableDataSerializer, pipeline, reporter);
    return;
}

ISpreadsheetDataSource spreadsheetDataSource = new GoogleSheetDataSource(options.CredentialsFile, options.SpreadsheetId);
List<ISpreadsheetDataSource> spreadsheetSources = new() { spreadsheetDataSource };
await RunFromSpreadsheetAsync(options, runMode, pipeline, reporter, spreadsheetSources);

static async Task RunFromSpreadsheetAsync(
    GeneratorAppOptions options,
    GeneratorRunMode runMode,
    ConfigGenerationPipeline pipeline,
    PipelineRunReporter reporter,
    List<ISpreadsheetDataSource> spreadsheetSources)
{
    if (runMode == GeneratorRunMode.Parse)
    {
        List<TableData> tables = await pipeline.ParseTablesAsync(spreadsheetSources);
        PrintLines(reporter.BuildParsingLines(tables));
        Console.WriteLine("Success");
        return;
    }

    if (runMode == GeneratorRunMode.Validate)
    {
        PipelineRunResult validated = await pipeline.ParseAndValidateDetailedAsync(spreadsheetSources);
        PrintValidationResult(validated, reporter);
        return;
    }

    PipelineRunResult runResult = await GenerateByModeFromSpreadsheetAsync(pipeline, runMode, spreadsheetSources, options.ResolveGeneratedFolder());

    if (runMode == GeneratorRunMode.GenerateArtifacts && runResult.IsSuccess)
    {
        MyConfig.Init(runResult.Tables);
    }

    PrintRunResult(runResult, options, reporter);
}

static async Task RunFromJsonAsync(
    GeneratorAppOptions options,
    GeneratorRunMode runMode,
    ITableDataSerializer tableDataSerializer,
    ConfigGenerationPipeline pipeline,
    PipelineRunReporter reporter)
{
    if (!File.Exists(options.JsonInputFile))
    {
        Console.WriteLine($"Json input file was not found: {options.JsonInputFile}");
        Console.WriteLine("Failure");
        return;
    }

    string json = await File.ReadAllTextAsync(options.JsonInputFile);
    List<TableData> tables = tableDataSerializer.Deserialize(json);

    if (runMode == GeneratorRunMode.Parse)
    {
        PrintLines(reporter.BuildParsingLines(tables));
        Console.WriteLine("Success");
        return;
    }

    OperationResult validation = pipeline.ValidateTables(tables);
    PipelineRunResult validated = new PipelineRunResult(tables, validation);

    if (runMode == GeneratorRunMode.Validate)
    {
        PrintValidationResult(validated, reporter);
        return;
    }

    PipelineRunResult runResult = GenerateByModeFromTables(pipeline, runMode, tables, options.ResolveGeneratedFolder());
    PrintRunResult(runResult, options, reporter);
}

static async Task<PipelineRunResult> GenerateByModeFromSpreadsheetAsync(
    ConfigGenerationPipeline pipeline,
    GeneratorRunMode runMode,
    List<ISpreadsheetDataSource> spreadsheetSources,
    string outputFolderPath)
{
    return runMode switch
    {
        GeneratorRunMode.GenerateCode => await pipeline.GenerateCodeDetailedAsync(spreadsheetSources, outputFolderPath),
        GeneratorRunMode.GenerateJson => await pipeline.GenerateJsonDetailedAsync(spreadsheetSources, outputFolderPath),
        _ => await pipeline.GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath),
    };
}

static PipelineRunResult GenerateByModeFromTables(
    ConfigGenerationPipeline pipeline,
    GeneratorRunMode runMode,
    List<TableData> tables,
    string outputFolderPath)
{
    return runMode switch
    {
        GeneratorRunMode.GenerateCode => pipeline.GenerateCodeFromTablesDetailed(tables, outputFolderPath),
        GeneratorRunMode.GenerateJson => pipeline.GenerateJsonFromTablesDetailed(tables, outputFolderPath),
        _ => pipeline.GenerateFromTablesDetailed(tables, outputFolderPath),
    };
}

static void PrintValidationResult(PipelineRunResult runResult, PipelineRunReporter reporter)
{
    PrintLines(reporter.BuildParsingLines(runResult.Tables));
    PrintLines(reporter.BuildValidationLines(runResult));
    Console.WriteLine(runResult.ValidationResult.IsSuccess ? "Success" : "Failure");
}

static void PrintRunResult(PipelineRunResult runResult, GeneratorAppOptions options, PipelineRunReporter reporter)
{
    if (options.PrintValidationIssues)
    {
        PrintLines(reporter.BuildValidationLines(runResult));
    }

    if (options.PrintGenerationMessages)
    {
        PrintLines(reporter.BuildGenerationLines(runResult));
    }

    Console.WriteLine(runResult.IsSuccess ? "Success" : "Failure");
}

static void PrintLines(List<string> lines)
{
    foreach (string line in lines)
    {
        Console.WriteLine(line);
    }
}
