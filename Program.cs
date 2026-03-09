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
using ConfigGenerator.ConfigInfrastructure.Validation;
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
    switch (runMode)
    {
        case GeneratorRunMode.Parse:
        {
            List<TableData> tables = await pipeline.ParseTablesAsync(spreadsheetSources);
            PrintLines(reporter.BuildParsingLines(tables));
            Console.WriteLine("Success");
            return;
        }

        case GeneratorRunMode.Validate:
        {
            PipelineRunResult validationResult = await pipeline.ParseAndValidateDetailedAsync(spreadsheetSources);
            PrintLines(reporter.BuildParsingLines(validationResult.Tables));
            PrintLines(reporter.BuildValidationLines(validationResult));
            Console.WriteLine(validationResult.ValidationResult.IsValid ? "Success" : "Failure");
            return;
        }

        case GeneratorRunMode.GenerateCode:
        {
            PipelineRunResult runResult = await pipeline.GenerateCodeDetailedAsync(spreadsheetSources, options.ResolveGeneratedFolder());
            PrintGenerationResult(runResult, options, reporter);
            return;
        }

        case GeneratorRunMode.GenerateJson:
        {
            PipelineRunResult runResult = await pipeline.GenerateJsonDetailedAsync(spreadsheetSources, options.ResolveGeneratedFolder());
            PrintGenerationResult(runResult, options, reporter);
            return;
        }

        default:
        {
            PipelineRunResult runResult = await pipeline.GenerateArtifactsDetailedAsync(spreadsheetSources, options.ResolveGeneratedFolder());
            if (runResult.IsSuccess)
            {
                MyConfig.Init(runResult.Tables);
            }

            PrintGenerationResult(runResult, options, reporter);
            return;
        }
    }
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

    ValidationResult validation = pipeline.ValidateTables(tables);
    PipelineRunResult validated = new PipelineRunResult(tables, validation);

    if (runMode == GeneratorRunMode.Validate)
    {
        PrintLines(reporter.BuildParsingLines(validated.Tables));
        PrintLines(reporter.BuildValidationLines(validated));
        Console.WriteLine(validated.ValidationResult.IsValid ? "Success" : "Failure");
        return;
    }

    PipelineRunResult runResult = runMode switch
    {
        GeneratorRunMode.GenerateCode => pipeline.GenerateCodeFromTablesDetailed(tables, options.ResolveGeneratedFolder()),
        GeneratorRunMode.GenerateJson => pipeline.GenerateJsonFromTablesDetailed(tables, options.ResolveGeneratedFolder()),
        _ => pipeline.GenerateFromTablesDetailed(tables, options.ResolveGeneratedFolder()),
    };

    PrintGenerationResult(runResult, options, reporter);
}

static void PrintGenerationResult(PipelineRunResult runResult, GeneratorAppOptions options, PipelineRunReporter reporter)
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
