using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigGenerator.ConfigInfrastructure.Data;
using ConfigGenerator.ConfigInfrastructure.Validation;
using ConfigGenerator.Parsing;
using ConfigGenerator.Spreadsheet;

namespace ConfigGenerator.Application;

public sealed class ConfigGenerationPipeline
{
    private readonly ConfigGenerator _configGenerator;
    private readonly ITableParser _tableParser;
    private readonly ITableValidator _tableValidator;

    public ConfigGenerationPipeline(
        ConfigGenerator configGenerator,
        ITableParser tableParser,
        ITableValidator tableValidator)
    {
        _configGenerator = configGenerator;
        _tableParser = tableParser;
        _tableValidator = tableValidator;
    }

    public async Task<List<TableData>> ParseTablesAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await _tableParser.ParseAsync(spreadsheetSources);
    }

    public ValidationResult ValidateTables(List<TableData> tables)
    {
        return _tableValidator.Validate(tables);
    }

    public async Task<PipelineRunResult> ParseAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        List<TableData> tables = await ParseTablesAsync(spreadsheetSources);
        return ValidateParsedTables(tables);
    }

    public async Task<PipelineRunResult> LoadAndValidateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        return await ParseAndValidateDetailedAsync(spreadsheetSources);
    }

    public async Task<List<TableData>> LoadAndValidateAsync(List<ISpreadsheetDataSource> spreadsheetSources)
    {
        PipelineRunResult result = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return result.ValidationResult.IsValid ? result.Tables : new List<TableData>();
    }

    public Task<PipelineRunResult> GenerateDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
    }

    public Task<PipelineRunResult> GenerateArtifactsDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return GenerateFromSourcesAsync(spreadsheetSources, outputFolderPath, _configGenerator.GenerateArtifactsDetailed);
    }

    public Task<PipelineRunResult> GenerateCodeDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return GenerateFromSourcesAsync(spreadsheetSources, outputFolderPath, _configGenerator.GenerateCodeDetailed);
    }

    public Task<PipelineRunResult> GenerateJsonDetailedAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        return GenerateFromSourcesAsync(spreadsheetSources, outputFolderPath, _configGenerator.GenerateJsonDetailed);
    }

    public async Task<bool> GenerateAsync(List<ISpreadsheetDataSource> spreadsheetSources, string outputFolderPath)
    {
        PipelineRunResult result = await GenerateArtifactsDetailedAsync(spreadsheetSources, outputFolderPath);
        return result.IsSuccess;
    }

    public PipelineRunResult GenerateFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateFromTablesDetailed(tables, outputFolderPath, _configGenerator.GenerateArtifactsDetailed);
    }

    public PipelineRunResult GenerateCodeFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateFromTablesDetailed(tables, outputFolderPath, _configGenerator.GenerateCodeDetailed);
    }

    public PipelineRunResult GenerateJsonFromTablesDetailed(List<TableData> tables, string outputFolderPath)
    {
        return GenerateFromTablesDetailed(tables, outputFolderPath, _configGenerator.GenerateJsonDetailed);
    }

    public bool GenerateFromTables(List<TableData> tables, string outputFolderPath)
    {
        PipelineRunResult result = GenerateFromTablesDetailed(tables, outputFolderPath);
        return result.IsSuccess;
    }

    private async Task<PipelineRunResult> GenerateFromSourcesAsync(
        List<ISpreadsheetDataSource> spreadsheetSources,
        string outputFolderPath,
        Func<List<TableData>, string, GenerationResult> generateAction)
    {
        PipelineRunResult parsed = await ParseAndValidateDetailedAsync(spreadsheetSources);
        return GenerateFromValidated(parsed, outputFolderPath, generateAction);
    }

    private PipelineRunResult GenerateFromTablesDetailed(
        List<TableData> tables,
        string outputFolderPath,
        Func<List<TableData>, string, GenerationResult> generateAction)
    {
        return GenerateFromValidated(ValidateParsedTables(tables), outputFolderPath, generateAction);
    }

    private PipelineRunResult ValidateParsedTables(List<TableData> tables)
    {
        ValidationResult validationResult = ValidateTables(tables);
        return new PipelineRunResult(tables, validationResult);
    }

    private static PipelineRunResult GenerateFromValidated(
        PipelineRunResult validated,
        string outputFolderPath,
        Func<List<TableData>, string, GenerationResult> generateAction)
    {
        if (!validated.ValidationResult.IsValid)
        {
            return CreateValidationFailedRunResult(validated);
        }

        GenerationResult generation = generateAction(validated.Tables, outputFolderPath);
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generation);
    }

    private static PipelineRunResult CreateValidationFailedRunResult(PipelineRunResult validated)
    {
        GenerationResult generationResult = new GenerationResult();
        generationResult.AddWarning("artifacts", "Generation skipped because validation failed.");
        return new PipelineRunResult(validated.Tables, validated.ValidationResult, generationResult);
    }
}
